using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using ErwinMayerLabs.RenameVSWindowTitle.Resolvers;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SolutionEvents = Microsoft.VisualStudio.Shell.Events.SolutionEvents;
using Task = System.Threading.Tasks.Task;

//If refactoring, do not change namespace as it'd cause existing settings to be lost.
namespace ErwinMayerLabs.RenameVSWindowTitle {
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "4.0", IconResourceID = 400)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(GlobalSettingsPageGrid), "Customize VS Window Title", "Global rules", 0, 0, true)]
    [ProvideOptionPage(typeof(SettingsOverridesPageGrid), "Customize VS Window Title", "Solution-specific overrides", 51, 500, true)]
    [ProvideOptionPage(typeof(SupportedTagsGrid), "Customize VS Window Title", "Supported tags", 101, 1000, true)]
    [Guid(GuidList.guidCustomizeVSWindowTitlePkgString)]
    public sealed class CustomizeVSWindowTitle : AsyncPackage {
        public CustomizeVSWindowTitle() {

            var productMajorPart = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo.ProductMajorPart;
            if (productMajorPart != 14)
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
            
            this.TagResolvers = new List<ITagResolver> {
                new DocumentNameResolver(),
                new ProjectNameResolver(),
                new StartupProjectNamesResolver(),
                new DocumentProjectNameResolver(),
                new DocumentProjectFileNameResolver(),
                new SolutionNameResolver(),
                new DocumentPathResolver(),
                new DocumentParentPathResolver(),
                new PathResolver(),
                new ParentPathResolver(),
                new ParentResolver(),
                new IdeNameResolver(),
                new ElevationSuffixResolver(),
                new VsMajorVersionResolver(),
                new VsMajorVersionYearResolver(),
                new PlatformNameResolver(),
                new ConfigurationNameResolver(),
                new GitBranchNameResolver(),
                new HgBranchNameResolver(),
                new SvnResolver(),
                new WorkspaceNameResolver(),
                new WorkspaceOwnerNameResolver(),
                new VsProcessIdResolver(),
                new EnvResolver(),
                new DebuggedProcessesArgsResolver(),
                new TfsBranchNameResolver()
            };
            this.SupportedTags = this.TagResolvers.SelectMany(r => r.TagNames).ToArray();
            this.SimpleTagResolvers = this.TagResolvers.OfType<ISimpleTagResolver>().ToDictionary(t => t.TagName, t => t);
        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            try {
                if (args.Name.StartsWith("Microsoft.TeamFoundation.") || args.Name.StartsWith("Microsoft.VisualStudio.Services.")) {
                    var aname = new System.Reflection.AssemblyName(args.Name);
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                        if (asm.GetName().Name == aname.Name)
                            return asm;
                    }
                }
            }
            catch {
                //Do nothing
            }
            return null;
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            CurrentPackage = this;
            Globals.DTE = (DTE2)GetGlobalService(typeof(DTE));
            Globals.DTE.Events.DebuggerEvents.OnEnterBreakMode += this.OnIdeEvent;
            Globals.DTE.Events.DebuggerEvents.OnEnterRunMode += this.OnIdeEvent;
            Globals.DTE.Events.DebuggerEvents.OnEnterDesignMode += this.OnIdeEvent;
            Globals.DTE.Events.DebuggerEvents.OnContextChanged += this.OnIdeEvent;
            Globals.DTE.Events.SolutionEvents.AfterClosing += this.OnIdeSolutionEvent;
            Globals.DTE.Events.SolutionEvents.Opened += this.OnIdeSolutionEvent;
            Globals.DTE.Events.SolutionEvents.Renamed += this.OnIdeSolutionEvent;
            Globals.DTE.Events.WindowEvents.WindowCreated += this.OnIdeEvent;
            Globals.DTE.Events.WindowEvents.WindowClosing += this.OnIdeEvent;
            Globals.DTE.Events.WindowEvents.WindowActivated += this.OnIdeEvent;
            Globals.DTE.Events.DocumentEvents.DocumentOpened += this.OnIdeEvent;
            Globals.DTE.Events.DocumentEvents.DocumentClosing += this.OnIdeEvent;

            this.GlobalSettingsWatcher.SettingsCleared = this.OnSettingsCleared;
            this.SolutionSettingsWatcher.SettingsCleared = this.OnSettingsCleared;

            //Every 5 seconds, we check the window titles in case we missed an event.
            this.ResetTitleTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            this.ResetTitleTimer.Tick += this.UpdateWindowTitleAsync;
            this.ResetTitleTimer.Start();
        }

        protected override void Dispose(bool disposing) {
            this.ResetTitleTimer.Dispose();
            base.Dispose(disposing: disposing);
        }

        #endregion

        private static readonly Regex TagRegex = new Regex(@"\[([^\[\]]+)\]", RegexOptions.Multiline | RegexOptions.Compiled);

        public string IDEName { get; private set; }
        public string ElevationSuffix { get; private set; }

        public static CustomizeVSWindowTitle CurrentPackage;

        private System.Windows.Forms.Timer ResetTitleTimer;
        private readonly List<ITagResolver> TagResolvers;
        private readonly Dictionary<string, ISimpleTagResolver> SimpleTagResolvers;

        public readonly string[] SupportedTags;

        private void OnIdeEvent(Window gotfocus, Window lostfocus) {
            this.OnIdeEvent();
        }

        private void OnIdeEvent(Document document) {
            this.OnIdeEvent();
        }

        private void OnIdeEvent(Window window) {
            this.OnIdeEvent();
        }

        private void OnIdeEvent(dbgEventReason reason) {
            this.OnIdeEvent();
        }

        private void OnIdeEvent(dbgEventReason reason, ref dbgExecutionAction executionaction) {
            this.OnIdeEvent();
        }

        private void OnIdeEvent(Process newProc, Program newProg, EnvDTE.Thread newThread, StackFrame newStkFrame) {
            this.OnIdeEvent();
        }

        private void OnIdeEvent() {
            if (this.UiSettings.EnableDebugMode) {
                WriteOutput("Debugger context changed. Updating title.");
            }
            this.UpdateWindowTitleAsync(this, EventArgs.Empty);
        }

        private void OnIdeSolutionEvent(string oldname) {
            this.ClearCachedSettings();
            this.OnIdeEvent();
        }

        // clear settings cache and update
        private void OnIdeSolutionEvent() {
            this.ClearCachedSettings();
            this.OnIdeEvent();
        }

        private GlobalSettingsPageGrid _UiSettings;

        internal GlobalSettingsPageGrid UiSettings {
            get {
                if (this._UiSettings == null) {
                    Globals.InvokeOnUIThread(() => {
                        this._UiSettings = this.GetDialogPage(typeof(GlobalSettingsPageGrid)) as GlobalSettingsPageGrid;  // as is faster than cast
                        this._UiSettings.SettingsChanged += (s, e) => this.OnIdeSolutionEvent();
                    });
                }
                return this._UiSettings;
            }
        }

        private SettingsOverridesPageGrid _UiSettingsOverridesOptions;

        internal SettingsOverridesPageGrid UiSettingsOverridesOptions {
            get {
                if (this._UiSettingsOverridesOptions == null) {
                    Globals.InvokeOnUIThread(() => {
                        this._UiSettingsOverridesOptions = this.GetDialogPage(typeof(SettingsOverridesPageGrid)) as SettingsOverridesPageGrid; // as is faster than cast
                        this._UiSettingsOverridesOptions.SettingsChanged += (s, e) => this.OnIdeSolutionEvent();
                    });
                }
                return this._UiSettingsOverridesOptions;
            }
        }

        private string GetIDEName(string str) {
            try {
                var m = new Regex(@"^(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.UiSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
                if (!m.Success) {
                    m = new Regex(@"^(.*) - (" + Globals.DTE.Name + @".* \(.+\)) \(.+\)$", RegexOptions.RightToLeft).Match(str);
                }
                if (!m.Success) {
                    m = new Regex(@"^(.*) - (" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
                }
                if (!m.Success) {
                    m = new Regex(@"^(" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
                }
                if (m.Success && m.Groups.Count >= 2) {
                    if (m.Groups.Count >= 3) {
                        return m.Groups[2].Captures[0].Value;
                    }
                    if (m.Groups.Count >= 2) {
                        return m.Groups[1].Captures[0].Value;
                    }
                }
                else {
                    if (this.UiSettings.EnableDebugMode) {
                        WriteOutput("IDE name (" + Globals.DTE.Name + ") not found: " + str + ".");
                    }
                    return null;
                }
            }
            catch (Exception ex) {
                if (this.UiSettings.EnableDebugMode) {
                    WriteOutput("GetIDEName Exception: " + str + ". Details: " + ex);
                }
                return null;
            }
            return "";
        }

        private string GetVSSolutionName(string str) {
            try {
                var m = new Regex(@"^(.*)\\(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.UiSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
                if (m.Success && m.Groups.Count >= 4) {
                    var name = m.Groups[2].Captures[0].Value;
                    var state = this.GetVSState(str);
                    return name.Substring(0, name.Length - (string.IsNullOrEmpty(state) ? 0 : state.Length + 3));
                }
                m = new Regex("^(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.UiSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
                if (m.Success && m.Groups.Count >= 3) {
                    var name = m.Groups[1].Captures[0].Value;
                    var state = this.GetVSState(str);
                    return name.Substring(0, name.Length - (string.IsNullOrEmpty(state) ? 0 : state.Length + 3));
                }
                m = new Regex("^(.*) - (" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
                if (m.Success && m.Groups.Count >= 3) {
                    var name = m.Groups[1].Captures[0].Value;
                    var state = this.GetVSState(str);
                    return name.Substring(0, name.Length - (string.IsNullOrEmpty(state) ? 0 : state.Length + 3));
                }
                if (this.UiSettings.EnableDebugMode) {
                    WriteOutput("VSName not found: " + str + ".");
                }
                return null;
            }
            catch (Exception ex) {
                if (this.UiSettings.EnableDebugMode) {
                    WriteOutput("GetVSName Exception: " + str + (". Details: " + ex));
                }
                return null;
            }
        }

        private string GetVSState(string str) {
            if (string.IsNullOrWhiteSpace(str)) return null;
            try {
                var m = new Regex(@" \((.*)\) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.UiSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
                if (!m.Success) {
                    m = new Regex(@" \((.*)\) - (" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
                }
                if (m.Success && m.Groups.Count >= 3) {
                    return m.Groups[1].Captures[0].Value;
                }
                if (this.UiSettings.EnableDebugMode) {
                    WriteOutput("VSState not found: " + str + ".");
                }
                return null;
            }
            catch (Exception ex) {
                if (this.UiSettings.EnableDebugMode) {
                    WriteOutput("GetVSState Exception: " + str + (". Details: " + ex));
                }
                return null;
            }
        }

        private void UpdateWindowTitleAsync(object state, EventArgs e) {
            if (this.IDEName == null && Globals.DTE.MainWindow != null) {
                this.IDEName = this.GetIDEName(Globals.DTE.MainWindow.Caption);
                if (!string.IsNullOrWhiteSpace(this.IDEName)) {
                    try {
                        var m = new Regex(@".*( \(.+\)).*$", RegexOptions.RightToLeft).Match(this.IDEName);
                        if (m.Success) {
                            this.ElevationSuffix = m.Groups[1].Captures[0].Value;
                        }
                    }
                    catch (Exception ex) {
                        if (this.UiSettings.EnableDebugMode) {
                            WriteOutput("UpdateWindowTitleAsync Exception: " + this.IDEName + (". Details: " + ex));
                        }
                    }
                }
            }
            if (this.IDEName == null) {
                return;
            }
            Task.Run(() => this.UpdateWindowTitle());
        }

        private readonly object UpdateWindowTitleLock = new object();

        private void UpdateWindowTitle() {
            if (!Monitor.TryEnter(this.UpdateWindowTitleLock)) {
                return;
            }
            try {
                var useDefaultPattern = true;
                if (this.UiSettings.AlwaysRewriteTitles) {
                    useDefaultPattern = false;
                }
                else {
                    Globals.GetVSMultiInstanceInfo(out var info);
                    if (info.nb_instances_same_solution >= 2) {
                        useDefaultPattern = false;
                    }
                    else {
                        var vsInstances = System.Diagnostics.Process.GetProcessesByName("devenv");
                        try {
                            if (vsInstances.Length >= 2) {
                                //Check if multiple instances of devenv have identical original names. If so, then rewrite the title of current instance (normally the extension will run on each instance so no need to rewrite them as well). Otherwise do not rewrite the title.
                                //The best would be to get the EnvDTE.DTE object of the other instances, and compare the solution or project names directly instead of relying on window titles (which may be hacked by third party software as well). But using moniker it will only work if they are launched with the same privilege.
                                var currentInstanceName = Path.GetFileNameWithoutExtension(Globals.DTE.Solution.FullName);
                                if (string.IsNullOrEmpty(currentInstanceName) || (from vsInstance in vsInstances
                                                                                  where vsInstance.Id != VsProcessIdResolver.VsProcessId.Value
                                                                                  select this.GetVSSolutionName(vsInstance.MainWindowTitle)).Any(vsInstanceName => vsInstanceName != null && currentInstanceName == vsInstanceName)) {
                                    useDefaultPattern = false;
                                }
                            }
                        }
                        finally {
                            foreach (var p in vsInstances) {
                                p.Dispose();
                            }
                        }
                    }
                }
                var solution = Globals.DTE.Solution;
                var solutionFp = solution?.FullName;

                var settings = this.GetSettings(solutionFp);

                var pattern = this.GetPattern(solutionFp, useDefaultPattern, settings);
                this.ChangeWindowTitle(this.GetNewTitle(solution, pattern, settings));
            }
            catch (Exception ex) {
                try {
                    if (this.UiSettings.EnableDebugMode) {
                        WriteOutput("UpdateWindowTitle exception: " + ex);
                    }
                }
                catch {
                    // ignored
                }
            }
            finally {
                Monitor.Exit(this.UpdateWindowTitleLock);
            }
        }

        private readonly SettingsWatcher SolutionSettingsWatcher = new SettingsWatcher(false);
        private readonly SettingsWatcher GlobalSettingsWatcher = new SettingsWatcher(true);

        private void ClearCachedSettings() {
            if (this.UiSettings.EnableDebugMode) {
                WriteOutput("Clearing cached settings...");
            }

            this.SolutionSettingsWatcher.Clear();
            this.GlobalSettingsWatcher.Clear();
            this.CachedSettings = null;
            if (this.UiSettings.EnableDebugMode) {
                WriteOutput("Clearing cached settings... Completed.");
            }
        }

        private void OnSettingsCleared() {
            this.CachedSettings = null; // force reload
        }

        private SettingsSet CachedSettings;
        internal SettingsSet GetSettings(string solutionFp) {
            this.GlobalSettingsWatcher.Update(this.UiSettingsOverridesOptions.GlobalSolutionSettingsOverridesFp);
            this.SolutionSettingsWatcher.Update(string.IsNullOrEmpty(solutionFp) ? null : solutionFp + Globals.SolutionSettingsOverrideExtension);

            // config already loaded, use cache
            if (this.CachedSettings != null && this.CachedSettings.SolutionFilePath == solutionFp) {
                return this.CachedSettings;
            }

            // init values from settings
            var settings = new SettingsSet {
                ClosestParentDepth = this.UiSettings.ClosestParentDepth,
                FarthestParentDepth = this.UiSettings.FarthestParentDepth,
                AppendedString = this.UiSettings.AppendedString,
                PatternIfBreakMode = this.UiSettings.PatternIfBreakMode,
                PatternIfDesignMode = this.UiSettings.PatternIfDesignMode,
                PatternIfRunningMode = this.UiSettings.PatternIfRunningMode,
            };

            if (!string.IsNullOrEmpty(solutionFp)) {
                settings.SolutionFilePath = solutionFp;
                settings.SolutionFileName = Path.GetFileName(solutionFp);
                settings.SolutionName = Path.GetFileNameWithoutExtension(solutionFp);

                if (!this.UiSettingsOverridesOptions.AllowSolutionSettingsOverrides) {
                    // Do nothing
                }
                else if (this.GlobalSettingsWatcher.Update(settings)) {
                    // Do nothing
                }
                else if (this.SolutionSettingsWatcher.Update(settings)) {
                    // Do nothing
                }
            }
            this.CachedSettings = settings;
            return settings;
        }

        public const string DefaultPatternIfDesignMode = "[solutionName] - [ideName]";
        public const string DefaultPatternIfBreakMode = "[solutionName] (Debugging) - [ideName]";
        public const string DefaultPatternIfRunningMode = "[solutionName] (Running) - [ideName]";
        public const string DefaultPatternIfDocumentButNoSolutionOpen = "[documentName] - [ideName]";
        public const string DefaultPatternIfNothingOpen = "[ideName]";
        public const string DefaultAppendedString = "*";
        public const int DefaultClosestParentDepth = 1;
        public const int DefaultFarthestParentDepth = 1;

        private string GetPattern(string solutionFp, bool useDefault, SettingsSet settingsOverride) {
            var settings = this.UiSettings;
            if (string.IsNullOrEmpty(solutionFp)) {
                var document = Globals.DTE.ActiveDocument;
                var window = Globals.DTE.ActiveWindow;
                if (string.IsNullOrEmpty(document?.FullName) && string.IsNullOrEmpty(window?.Caption)) {
                    return useDefault ? DefaultPatternIfNothingOpen : settings.PatternIfNothingOpen;
                }
                return useDefault ? DefaultPatternIfDocumentButNoSolutionOpen : settings.PatternIfDocumentButNoSolutionOpen;
            }
            string designModePattern = null;
            string breakModePattern = null;
            string runningModePattern = null;
            if (!useDefault) {
                designModePattern = settingsOverride?.PatternIfDesignMode ?? settings.PatternIfDesignMode;
                breakModePattern = settingsOverride?.PatternIfBreakMode ?? settings.PatternIfBreakMode;
                runningModePattern = settingsOverride?.PatternIfRunningMode ?? settings.PatternIfRunningMode;
            }
            if (Globals.DTE.Debugger == null || Globals.DTE.Debugger.CurrentMode == dbgDebugMode.dbgDesignMode) {
                return designModePattern ?? DefaultPatternIfDesignMode;
            }
            if (Globals.DTE.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode) {
                return breakModePattern ?? DefaultPatternIfBreakMode;
            }
            if (Globals.DTE.Debugger.CurrentMode == dbgDebugMode.dbgRunMode) {
                return runningModePattern ?? DefaultPatternIfRunningMode;
            }
            throw new Exception("No matching state found");
        }

        internal string GetNewTitle(Solution solution, string pattern, SettingsSet cfg) {
            var info = AvailableInfo.GetCurrent(ideName: this.IDEName, solution: solution, cfg: cfg, globalSettings: this.UiSettings);
            if (info == null) return this.IDEName;

            pattern = TagRegex.Replace(pattern, match => {
                try {
                    var tag = match.Groups[1].Value;
                    try {
                        if (this.SimpleTagResolvers.TryGetValue(tag, out var resolver)) {
                            return resolver.Resolve(info: info);
                        }
                        foreach (var tagResolver in this.TagResolvers) {
                            if (tagResolver.TryResolve(tag: tag, info: info, s: out var value)) {
                                return value;
                            }
                        }
                        return match.Value;
                    }
                    catch (Exception ex) {
                        if (this.UiSettings.EnableDebugMode) {
                            WriteOutput("ReplaceTag (" + tag + ") failed: " + ex);
                        }
                        throw;
                    }
                }
                catch {
                    return "";
                }
            });
            var appendedString = cfg?.AppendedString ?? this.UiSettings.AppendedString;
            return pattern + " " + appendedString;
        }

        private void ChangeWindowTitle(string title) {
            try {
                Globals.BeginInvokeOnUIThread(() => {
                    try {
                        System.Windows.Application.Current.MainWindow.Title = Globals.DTE.MainWindow.Caption;
                        if (System.Windows.Application.Current.MainWindow.Title != title) {
                            System.Windows.Application.Current.MainWindow.Title = title;
                        }
                        //foreach (var windowObj in System.Windows.Application.Current.Windows) {
                        //    var window = windowObj as Microsoft.VisualStudio.PlatformUI.Shell.Controls.FloatingWindow;
                        //    if (window != null) {
                        //        window.Title = Globals.DTE.MainWindow.Caption;
                        //        if (window.Title != title) {
                        //            window.Title = title;
                        //        }
                        //    }
                        //}
                    }
                    catch (Exception) {
                        // ignored
                    }
                });
            }
            catch (Exception ex) {
                if (this.UiSettings.EnableDebugMode) {
                    WriteOutput("ChangeWindowTitle failed: " + ex);
                }
            }
        }

        public static void WriteOutput(string str, params object[] args) {
            try {
                Globals.InvokeOnUIThread(() => {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    var generalPaneGuid = VSConstants.OutputWindowPaneGuid.DebugPane_guid;
                    // P.S. There's also the VSConstants.GUID_OutWindowDebugPane available.
                    if (GetGlobalService(typeof(SVsOutputWindow)) is IVsOutputWindow outWindow) {
                        outWindow.GetPane(ref generalPaneGuid, out IVsOutputWindowPane generalPane);
                        generalPane.OutputString("CustomizeVSWindowTitle: " + string.Format(str, args) + "\r\n");
                        generalPane.Activate();
                    }
                });
            }
            catch {
                // ignored
            }
        }
    }
}
