using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using ErwinMayerLabs.Lib;

// The PackageRegistration attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class
// is a package.
//
// The InstalledProductRegistration attribute is used to register the information needed to show this package
// in the Help/About dialog of Visual Studio.

namespace ErwinMayerLabs.RenameVSWindowTitle {
    [PackageRegistration(UseManagedResourcesOnly = true), InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400), ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string), ProvideMenuResource("Menus.ctmenu", 1), Guid(GuidList.guidRenameVSWindowTitle2PkgString)]
    [ProvideOptionPage(typeof(GlobalSettingsPageGrid), "Rename VS Window Title", "Global rules", 0, 0, true)]
    [ProvideOptionPage(typeof(SettingsOverridesPageGrid), "Rename VS Window Title", "Solution-specific overrides", 51, 500, true)]
    [ProvideOptionPage(typeof(SupportedTagsGrid), "Rename VS Window Title", "Supported tags", 101, 1000, true)]
    public sealed class RenameVSWindowTitle : Package {
        private string IDEName;

        public static RenameVSWindowTitle CurrentPackage;

        private System.Windows.Forms.Timer ResetTitleTimer;
        //Private VersionSpecificAssembly As Assembly

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require
        /// any Visual Studio service because at this point the package object is created but
        /// not sited yet inside Visual Studio environment. The place to do all the other
        /// initialization is the Initialize method.
        /// </summary>
        public RenameVSWindowTitle() {
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
        }

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
            if (this.GlobalSettings.EnableDebugMode) {
                WriteOutput("Debugger context changed. Updating title.");
            }
            this.UpdateWindowTitleAsync(this, EventArgs.Empty);
        }

        private void OnIdeSolutionEvent(string oldname) {
            this.ClearSettingsCache();
            this.OnIdeEvent();
        }

        // clear settings cache and update
        private void OnIdeSolutionEvent() {
            this.ClearSettingsCache();
            this.OnIdeEvent();
        }

        ///'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        // Overriden Package Implementation

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize() {
            base.Initialize();
            CurrentPackage = this;

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


        private GlobalSettingsPageGrid _GlobalSettings;

        internal GlobalSettingsPageGrid GlobalSettings {
            get {
                if (this._GlobalSettings == null) {
                    this._GlobalSettings = this.GetDialogPage(typeof(GlobalSettingsPageGrid)) as GlobalSettingsPageGrid;  // as is faster than cast
                    this._GlobalSettings.SettingsChanged += (s, e) => this.OnIdeSolutionEvent();
                }
                return this._GlobalSettings;
            }
        }

        private SettingsOverridesPageGrid _SettingsOverrides;

        internal SettingsOverridesPageGrid SettingsOverrides {
            get {
                if (this._SettingsOverrides == null) {
                    this._SettingsOverrides = this.GetDialogPage(typeof(SettingsOverridesPageGrid)) as SettingsOverridesPageGrid;  // as is faster than cast
                    this._SettingsOverrides.SettingsChanged += (s, e) => this.OnIdeSolutionEvent();
                }
                return this._SettingsOverrides;
            }
        }

        private string GetIDEName(string str) {
            try {
                var m = new Regex(@"^(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.GlobalSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
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
                    if (this.GlobalSettings.EnableDebugMode) {
                        WriteOutput("IDE name (" + Globals.DTE.Name + ") not found: " + str + ".");
                    }
                    return null;
                }
            }
            catch (Exception ex) {
                if (this.GlobalSettings.EnableDebugMode) {
                    WriteOutput("GetIDEName Exception: " + str + (". Details: " + ex));
                }
                return null;
            }
            return "";
        }

        private string GetVSSolutionName(string str) {
            try {
                var m = new Regex(@"^(.*)\\(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.GlobalSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
                if (m.Success && m.Groups.Count >= 4) {
                    var name = m.Groups[2].Captures[0].Value;
                    var state = this.GetVSState(str);
                    return name.Substring(0, name.Length - (string.IsNullOrEmpty(state) ? 0 : state.Length + 3));
                }
                m = new Regex("^(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.GlobalSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
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
                if (this.GlobalSettings.EnableDebugMode) {
                    WriteOutput("VSName not found: " + str + ".");
                }
                return null;
            }
            catch (Exception ex) {
                if (this.GlobalSettings.EnableDebugMode) {
                    WriteOutput("GetVSName Exception: " + str + (". Details: " + ex));
                }
                return null;
            }
        }

        private string GetVSState(string str) {
            try {
                var m = new Regex(@" \((.*)\) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.GlobalSettings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
                if (!m.Success) {
                    m = new Regex(@" \((.*)\) - (" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
                }
                if (m.Success && m.Groups.Count >= 3) {
                    return m.Groups[1].Captures[0].Value;
                }
                if (this.GlobalSettings.EnableDebugMode) {
                    WriteOutput("VSState not found: " + str + ".");
                }
                return null;
            }
            catch (Exception ex) {
                if (this.GlobalSettings.EnableDebugMode) {
                    WriteOutput("GetVSState Exception: " + str + (". Details: " + ex));
                }
                return null;
            }
        }

        private void UpdateWindowTitleAsync(object state, EventArgs e) {
            if (this.IDEName == null && Globals.DTE.MainWindow != null) {
                this.IDEName = this.GetIDEName(Globals.DTE.MainWindow.Caption);
            }
            if (this.IDEName == null) {
                return;
            }
            System.Threading.Tasks.Task.Factory.StartNew(this.UpdateWindowTitle);
        }

        private readonly object UpdateWindowTitleLock = new object();

        private void UpdateWindowTitle() {
            if (!Monitor.TryEnter(this.UpdateWindowTitleLock)) {
                return;
            }
            try {
                var useDefaultPattern = true;
                if (this.GlobalSettings.AlwaysRewriteTitles) {
                    useDefaultPattern = false;
                }
                else {
                    Globals.VSMultiInstanceInfo info;
                    Globals.GetVSMultiInstanceInfo(out info);
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
                                                                                  where vsInstance.Id != Globals.VsProcessId.Value
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
                    if (this.GlobalSettings.EnableDebugMode) {
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

        readonly SettingsWatcher SolutionSettingsWatcher = new SettingsWatcher(false);
        readonly SettingsWatcher GlobalSettingsWatcher = new SettingsWatcher(true);
        SettingsSet CurrentSettingsOverride;

        private void ClearSettingsCache() {
            if (this.GlobalSettings.EnableDebugMode) {
                WriteOutput("ClearSettingsCache.");
            }

            this.SolutionSettingsWatcher.Clear();
            this.GlobalSettingsWatcher.Clear();
            this.CurrentSettingsOverride = null;
        }

        private void OnSettingsCleared() {
            this.CurrentSettingsOverride = null; // force reload
        }

        internal SettingsSet GetSettings(string solutionFp) {
            this.GlobalSettingsWatcher.Update(this.SettingsOverrides.GlobalSolutionSettingsOverridesFp);
            this.SolutionSettingsWatcher.Update(string.IsNullOrEmpty(solutionFp) ? null : solutionFp + Globals.SolutionSettingsOverrideExtension);

            // config already loaded, use cache
            if (this.CurrentSettingsOverride != null && this.CurrentSettingsOverride.SolutionFilePath == solutionFp) {
                return this.CurrentSettingsOverride;
            }

            var settings = this.GlobalSettings;

            // init values from settings
            this.CurrentSettingsOverride = new SettingsSet {
                ClosestParentDepth = settings.ClosestParentDepth,
                FarthestParentDepth = settings.FarthestParentDepth,
                AppendedString = settings.AppendedString,
                ElevatedString = settings.ElevatedString,
                PatternIfBreakMode = settings.PatternIfBreakMode,
                PatternIfDesignMode = settings.PatternIfDesignMode,
                PatternIfRunningMode = settings.PatternIfRunningMode,
            };
            if (string.IsNullOrEmpty(solutionFp))
                return this.CurrentSettingsOverride;

            this.CurrentSettingsOverride.SolutionFilePath = solutionFp;
            this.CurrentSettingsOverride.SolutionFileName = Path.GetFileName(solutionFp);
            this.CurrentSettingsOverride.SolutionName = Path.GetFileNameWithoutExtension(solutionFp);

            // no override allowed, return
            if (!this.SettingsOverrides.AllowSolutionSettingsOverrides)
                return this.CurrentSettingsOverride;

            // check global override file
            if (this.GlobalSettingsWatcher.Update(this.CurrentSettingsOverride)) {
                return this.CurrentSettingsOverride;
            }

            // check solution override file
            if (this.SolutionSettingsWatcher.Update(this.CurrentSettingsOverride)) {
                return this.CurrentSettingsOverride;
            }
            return this.CurrentSettingsOverride;
        }

        public const string DefaultPatternIfDesignMode = "[solutionName] - [ideName]";
        public const string DefaultPatternIfBreakMode = "[solutionName] (Debugging) - [ideName]";
        public const string DefaultPatternIfRunningMode = "[solutionName] (Running) - [ideName]";
        public const string DefaultPatternIfDocumentButNoSolutionOpen = "[documentName] - [ideName]";
        public const string DefaultPatternIfNothingOpen = "[ideName]";
        public const string DefaultAppendedString = "*";
        public const string DefaultElevatedString = "(elevated)";
        public const int DefaultClosestParentDepth = 1;
        public const int DefaultFarthestParentDepth = 1;

        private string GetPattern(string solutionFp, bool useDefault, SettingsSet settingsOverride) {
            var Settings = this.GlobalSettings;
            if (string.IsNullOrEmpty(solutionFp)) {
                var document = Globals.DTE.ActiveDocument;
                var window = Globals.DTE.ActiveWindow;
                if (string.IsNullOrEmpty(document?.FullName) && string.IsNullOrEmpty(window?.Caption)) {
                    return useDefault ? DefaultPatternIfNothingOpen : Settings.PatternIfNothingOpen;
                }
                return useDefault ? DefaultPatternIfDocumentButNoSolutionOpen : Settings.PatternIfDocumentButNoSolutionOpen;
            }
            string designModePattern = null;
            string breakModePattern = null;
            string runningModePattern = null;
            if (!useDefault) {
                designModePattern = settingsOverride?.PatternIfDesignMode ?? Settings.PatternIfDesignMode;
                breakModePattern = settingsOverride?.PatternIfBreakMode ?? Settings.PatternIfBreakMode;
                runningModePattern = settingsOverride?.PatternIfRunningMode ?? Settings.PatternIfRunningMode;
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

        public static readonly string[] SupportedTags = {
            "documentName",
            "projectName",
            "documentProjectName",
            "documentProjectFileName",
            "solutionName",
            "documentPath",
            "documentPath:X",
            "documentPath:X:Y",
            "documentParentPath:X",
            "documentParentPath:X:Y",
            "path",
            "path:X",
            "path:X:Y",
            "parentPath",
            "parent:X",
            "parent:X:Y",
            "ideName",
            "elevated",
            "vsMajorVersion",
            "vsMajorVersionYear",
            "platformName",
            "configurationName",
            "gitBranchName",
            "hgBranchName",
            "workspaceName",
            "workspaceOwnerName",
            "vsProcessID",
            "env:X"
        };

        readonly Regex TagRegex = new Regex(@"\[([^\[\]]+)\]", RegexOptions.Multiline | RegexOptions.Compiled);

        internal string GetNewTitle(Solution solution, string pattern, SettingsSet cfg) {
            Document activeDocument = null;
            Window activeWindow = null;
            try {
                activeDocument = Globals.DTE.ActiveDocument;
            }
            catch {
                // Do nothing
            }
            try {
                activeWindow = Globals.DTE.ActiveWindow;
            }
            catch {
                // Do nothing
            }
            var solutionFp = solution?.FullName;
            if (activeDocument == null && string.IsNullOrEmpty(solutionFp)) {
                if (activeWindow == null || activeWindow.Caption == Globals.DTE.MainWindow.Caption) {
                    return this.IDEName;
                }
            }
            string path;
            var documentName = Globals.GetActiveDocumentNameOrEmpty(activeDocument);
            var documentPath = Globals.GetActiveDocumentPathOrEmpty(activeDocument);
            var windowName = Globals.GetActiveWindowNameOrEmpty(activeWindow);

            if (!string.IsNullOrEmpty(solutionFp)) {
                path = solutionFp;
            }
            else {
                path = documentPath;
            }

            var pathParts = this.SplitPath(path);
            if (!string.IsNullOrEmpty(path)) {
                pathParts[0] = Path.GetPathRoot(path).Replace("\\", "");
            }

            var documentPathParts = this.SplitPath(documentPath);
            if (!string.IsNullOrEmpty(documentPath)) {
                documentPathParts[0] = Path.GetPathRoot(documentPath).Replace("\\", "");
            }

            pattern = this.TagRegex.Replace(pattern, match => {
                try {
                    var tag = match.Groups[1].Value;
                    try {
                        switch (tag) {
                            case "configurationName":
                                return Globals.GetActiveConfigurationNameOrEmpty(solution);
                            case "platformName":
                                return Globals.GetPlatformNameOrEmpty(solution);
                            case "projectName":
                                return Globals.GetActiveProjectNameOrEmpty();
                            case "solutionName":
                                return cfg.SolutionName ?? string.Empty;
                            case "gitBranchName":
                                Globals.UpdateGitExecFp(this.GlobalSettings.GitDirectory); // there is likely a better way to adjust the git path
                                return Globals.GetGitBranchNameOrEmpty(solution);
                            case "hgBranchName":
                                Globals.UpdateHgExecFp(this.GlobalSettings.HgDirectory);
                                return Globals.GetHgBranchNameOrEmpty(solution);
                            case "workspaceName":
                                return Globals.GetWorkspaceNameOrEmpty(solution);
                            case "workspaceOwnerName":
                                return Globals.GetWorkspaceOwnerNameOrEmpty(solution);
                            case "documentName":
                                return string.IsNullOrEmpty(documentName) ? windowName : documentName;
                            case "documentProjectName":
                                return Globals.GetActiveDocumentProjectNameOrEmpty(activeDocument: activeDocument);
                            case "documentProjectFileName":
                                return Globals.GetActiveDocumentProjectFileNameOrEmpty(activeDocument: activeDocument);
                            case "documentPath":
                                return string.IsNullOrEmpty(documentName) ? windowName : documentPath;
                            case "vsMajorVersion":
                                return Globals.VsMajorVersion.ToString(CultureInfo.InvariantCulture);
                            case "vsMajorVersionYear":
                                return Globals.VsMajorVersionYear.ToString(CultureInfo.InvariantCulture);
                            case "vsProcessID":
                                return Globals.VsProcessId.Value.ToString(CultureInfo.InvariantCulture);
                            case "ideName":
                                return this.IDEName ?? string.Empty;
                            case "elevated":
                                return Elevated.IsElevated() ? (" " + cfg?.ElevatedString ?? this.GlobalSettings.ElevatedString) : string.Empty;
                            case "path":
                                return string.IsNullOrEmpty(path) ? windowName : path;
                            case "parentPath":
                                return GetParentPath(pathParts, cfg?.ClosestParentDepth ?? this.GlobalSettings.ClosestParentDepth, cfg?.FarthestParentDepth ?? this.GlobalSettings.FarthestParentDepth) ?? string.Empty;
                            default: 
                                if (tag.StartsWith("parent")) {
                                    var m = RangeRegex.Match(tag.Substring("parent".Length));
                                    if (m.Success) {
                                        if (!pathParts.Any()) return string.Empty;
                                        var startIndex = Math.Min(pathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["startIndex"].Value, CultureInfo.InvariantCulture)));
                                        var endIndex = Math.Min(pathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["endIndex"].Value, CultureInfo.InvariantCulture)));
                                        var pathRange = pathParts.GetRange(startIndex: pathParts.Length - 1 - startIndex, endIndex: pathParts.Length - 1 - endIndex).ToArray();
                                        return GetPathForTitle(pathRange);
                                    }
                                    m = IndexRegex.Match(tag.Substring("parent".Length));
                                    if (m.Success) {
                                        if (!pathParts.Any()) return string.Empty;
                                        var index = Math.Min(pathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["index"].Value, CultureInfo.InvariantCulture)));
                                        return pathParts[pathParts.Length - 1 - index];
                                    }
                                }
                                if (tag.StartsWith("path")) {
                                    var m = RangeRegex.Match(tag.Substring("path".Length));
                                    if (m.Success) {
                                        if (!pathParts.Any()) return string.Empty;
                                        var startIndex = Math.Min(pathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["startIndex"].Value, CultureInfo.InvariantCulture)));
                                        var endIndex = Math.Min(pathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["endIndex"].Value, CultureInfo.InvariantCulture)));
                                        var pathRange = pathParts.GetRange(startIndex: startIndex, endIndex: endIndex).ToArray();
                                        return GetPathForTitle(pathRange);
                                    }
                                    m = IndexRegex.Match(tag.Substring("path".Length));
                                    if (m.Success) {
                                        if (!pathParts.Any()) return string.Empty;
                                        var index = Math.Min(pathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["index"].Value, CultureInfo.InvariantCulture)));
                                        return pathParts[index];
                                    }
                                }
                                if (tag.StartsWith("documentPath")) {
                                    var m = RangeRegex.Match(tag.Substring("documentPath".Length));
                                    if (m.Success) {
                                        if (!documentPathParts.Any()) return string.Empty;
                                        var startIndex = Math.Min(documentPathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["startIndex"].Value, CultureInfo.InvariantCulture)));
                                        var endIndex = Math.Min(documentPathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["endIndex"].Value, CultureInfo.InvariantCulture)));
                                        var pathRange = documentPathParts.GetRange(startIndex: startIndex, endIndex: endIndex).ToArray();
                                        return GetPathForTitle(pathRange);
                                    }
                                    m = IndexRegex.Match(tag.Substring("documentPath".Length));
                                    if (m.Success) {
                                        if (!documentPathParts.Any()) return string.Empty;
                                        var index = Math.Min(documentPathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["index"].Value, CultureInfo.InvariantCulture)));
                                        return documentPathParts[index];
                                    }
                                }
                                if (tag.StartsWith("documentParentPath")) {
                                    var m = RangeRegex.Match(tag.Substring("documentParentPath".Length));
                                    if (m.Success) {
                                        if (!documentPathParts.Any()) return string.Empty;
                                        var startIndex = Math.Min(documentPathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["startIndex"].Value, CultureInfo.InvariantCulture)));
                                        var endIndex = Math.Min(documentPathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["endIndex"].Value, CultureInfo.InvariantCulture)));
                                        var pathRange = documentPathParts.GetRange(startIndex: documentPathParts.Length - 1 - startIndex, endIndex: documentPathParts.Length - 1 - endIndex).ToArray();
                                        return GetPathForTitle(pathRange);
                                    }
                                    m = IndexRegex.Match(tag.Substring("documentParentPath".Length));
                                    if (m.Success) {
                                        if (!documentPathParts.Any()) return string.Empty;
                                        var index = Math.Min(documentPathParts.Length - 1, Math.Max(0, int.Parse(m.Groups["index"].Value, CultureInfo.InvariantCulture)));
                                        return documentPathParts[documentPathParts.Length - 1 - index];
                                    }
                                } 
                                {
                                    var m = EnvRegex.Match(tag);
                                    if (m.Success) {
                                        return Environment.GetEnvironmentVariable(m.Groups[1].Value);
                                    }
                                }
                                break;
                        }
                        return match.Value;
                    }
                    catch (Exception ex) {
                        if (this.GlobalSettings.EnableDebugMode) {
                            WriteOutput("ReplaceTag (" + tag + ") failed: " + ex);
                        }
                        throw;
                    }
                }
                catch {
                    return "";
                }
            });
            var appendedString = cfg?.AppendedString ?? this.GlobalSettings.AppendedString;
            return pattern + " " + appendedString;
        }

        static readonly Regex EnvRegex = new Regex(@"^env:(.+)$", RegexOptions.Compiled);
        static readonly Regex IndexRegex = new Regex(@"^:?(?<index>[0-9]+)$", RegexOptions.Compiled);
        static readonly Regex RangeRegex = new Regex(@"^:(?<startIndex>[0-9]+):(?<endIndex>[0-9]+)$", RegexOptions.Compiled);

        private static string GetPathForTitle(string[] pathRange) {
            if (pathRange.Any()) {
                if (pathRange.Length >= 2 && pathRange[0].EndsWith(":")) {
                    pathRange = pathRange.ToArray();
                    pathRange[0] += Path.DirectorySeparatorChar;
                }
                return Path.Combine(pathRange);
            }
            return string.Empty;
        }

        private string[] SplitPath(string path) {
            if (string.IsNullOrEmpty(path)) {
                return new string[0];
            }

            var root = Path.GetPathRoot(path);
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(root)) {
                parts.Add(root);
            }
            parts.AddRange(path.Substring(root.Length).Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries));
            return parts.ToArray();
        }

        private static string GetParentPath(string[] pathParts, int closestParentDepth, int farthestParentDepth) {
            if (closestParentDepth > farthestParentDepth) {
                // swap if provided in wrong order
                var t = closestParentDepth;
                closestParentDepth = farthestParentDepth;
                farthestParentDepth = t;
            }
            return Path.Combine(pathParts.Reverse().Skip(closestParentDepth)
                .Take(farthestParentDepth - closestParentDepth + 1)
                .Reverse()
                .ToArray());
        }

        private void ChangeWindowTitle(string title) {
            try {
                Globals.BeginInvokeOnUIThread(() => {
                    try {
                        System.Windows.Application.Current.MainWindow.Title = Globals.DTE.MainWindow.Caption;
                        if (System.Windows.Application.Current.MainWindow.Title != title) {
                            System.Windows.Application.Current.MainWindow.Title = title;
                        }
                    }
                    catch (Exception) {
                        // ignored
                    }
                });
            }
            catch (Exception ex) {
                if (this.GlobalSettings.EnableDebugMode) {
                    WriteOutput("ChangeWindowTitle failed: " + ex);
                }
            }
        }

        private static void WriteOutput(string str, params object[] args) {
            try {
                Globals.InvokeOnUIThread(() => {
                    var outWindow = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                    var generalPaneGuid = VSConstants.OutputWindowPaneGuid.DebugPane_guid;
                    // P.S. There's also the VSConstants.GUID_OutWindowDebugPane available.
                    if (outWindow != null) {
                        IVsOutputWindowPane generalPane;
                        outWindow.GetPane(ref generalPaneGuid, out generalPane);
                        generalPane.OutputString("RenameVSWindowTitle: " + string.Format(str, args) + "\r\n");
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