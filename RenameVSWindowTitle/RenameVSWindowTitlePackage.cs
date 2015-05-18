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

// The PackageRegistration attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class
// is a package.
//
// The InstalledProductRegistration attribute is used to register the information needed to show this package
// in the Help/About dialog of Visual Studio.

namespace ErwinMayerLabs.RenameVSWindowTitle {
    [PackageRegistration(UseManagedResourcesOnly = true), InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400), ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string), ProvideMenuResource("Menus.ctmenu", 1), Guid(GuidList.guidRenameVSWindowTitle2PkgString)]
    [ProvideOptionPage(typeof(OptionPageGrid), "Rename VS Window Title", "Rules", 0, 0, true)]
    [ProvideOptionPage(typeof(SupportedTagsGrid), "Rename VS Window Title", "Supported tags", 101, 1000, true)]
    public sealed class RenameVSWindowTitle : Package {
        private string IDEName;

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
            Globals.DTE = (DTE2)(GetGlobalService(typeof(DTE)));
            Globals.DTE.Events.DebuggerEvents.OnEnterBreakMode += this.OnIdeEvent;
            Globals.DTE.Events.DebuggerEvents.OnEnterRunMode += this.OnIdeEvent;
            Globals.DTE.Events.DebuggerEvents.OnEnterDesignMode += this.OnIdeEvent;
            Globals.DTE.Events.DebuggerEvents.OnContextChanged += this.OnIdeEvent;
            Globals.DTE.Events.SolutionEvents.AfterClosing += this.OnIdeEvent;
            Globals.DTE.Events.SolutionEvents.Opened += this.OnIdeEvent;
            Globals.DTE.Events.SolutionEvents.Renamed += this.OnIdeEvent;
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

        private void OnIdeEvent(string oldname) {
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
            if (this.Settings.EnableDebugMode) {
                WriteOutput("Debugger context changed. Updating title.");
            }
            this.UpdateWindowTitleAsync(this, EventArgs.Empty);
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

        private OptionPageGrid Settings {
            get { return ((OptionPageGrid)(this.GetDialogPage(typeof(OptionPageGrid)))); }
        }

        private string GetIDEName(string str) {
            try {
                var m = new Regex(@"^(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
                if (!m.Success) {
                    m = new Regex(@"^(.*) - (" + Globals.DTE.Name + @".* \(.+\)) \(.+\)$", RegexOptions.RightToLeft).Match(str);
                }
                if (!m.Success) {
                    m = new Regex(@"^(.*) - (" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
                }
                if (!m.Success) {
                    m = new Regex(@"^(" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
                }
                if ((m.Success) && m.Groups.Count >= 2) {
                    if (m.Groups.Count >= 3) {
                        return m.Groups[2].Captures[0].Value;
                    }
                    if (m.Groups.Count >= 2) {
                        return m.Groups[1].Captures[0].Value;
                    }
                }
                else {
                    if (this.Settings.EnableDebugMode) {
                        WriteOutput("IDE name (" + Globals.DTE.Name + ") not found: " + str + ".");
                    }
                    return null;
                }
            }
            catch (Exception ex) {
                if (this.Settings.EnableDebugMode) {
                    WriteOutput("GetIDEName Exception: " + str + (". Details: " + ex));
                }
                return null;
            }
            return "";
        }

        private string GetVSSolutionName(string str) {
            try {
                var m = new Regex(@"^(.*)\\(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
                if ((m.Success) && m.Groups.Count >= 4) {
                    var name = m.Groups[2].Captures[0].Value;
                    var state = this.GetVSState(str);
                    return name.Substring(0, name.Length - ((string.IsNullOrEmpty(state)) ? 0 : state.Length + 3));
                }
                m = new Regex("^(.*) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
                if ((m.Success) && m.Groups.Count >= 3) {
                    var name = m.Groups[1].Captures[0].Value;
                    var state = this.GetVSState(str);
                    return name.Substring(0, name.Length - ((string.IsNullOrEmpty(state)) ? 0 : state.Length + 3));
                }
                m = new Regex("^(.*) - (" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
                if ((m.Success) && m.Groups.Count >= 3) {
                    var name = m.Groups[1].Captures[0].Value;
                    var state = this.GetVSState(str);
                    return name.Substring(0, name.Length - ((string.IsNullOrEmpty(state)) ? 0 : state.Length + 3));
                }
                if (this.Settings.EnableDebugMode) {
                    WriteOutput("VSName not found: " + str + ".");
                }
                return null;
            }
            catch (Exception ex) {
                if (this.Settings.EnableDebugMode) {
                    WriteOutput("GetVSName Exception: " + str + (". Details: " + ex));
                }
                return null;
            }
        }

        private string GetVSState(string str) {
            try {
                var m = new Regex(@" \((.*)\) - (" + Globals.DTE.Name + ".*) " + Regex.Escape(this.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str);
                if (!m.Success) {
                    m = new Regex(@" \((.*)\) - (" + Globals.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str);
                }
                if ((m.Success) && m.Groups.Count >= 3) {
                    return m.Groups[1].Captures[0].Value;
                }
                if (this.Settings.EnableDebugMode) {
                    WriteOutput("VSState not found: " + str + ".");
                }
                return null;
            }
            catch (Exception ex) {
                if (this.Settings.EnableDebugMode) {
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
                var rewrite = false;
                if (this.Settings.AlwaysRewriteTitles) {
                    rewrite = true;
                }
                else {
                    Globals.VSMultiInstanceInfo info;
                    Globals.GetVSMultiInstanceInfo(out info);
                    if (info.nb_instances_same_solution >= 2) {
                        rewrite = true;
                    }
                    else {
                        var currentInstance = System.Diagnostics.Process.GetCurrentProcess();
                        var vsInstances = System.Diagnostics.Process.GetProcessesByName("devenv");
                        if (vsInstances.Count() >= 2) {
                            //Check if multiple instances of devenv have identical original names. If so, then rewrite the title of current instance (normally the extension will run on each instance so no need to rewrite them as well). Otherwise do not rewrite the title.
                            //The best would be to get the EnvDTE.DTE object of the other instances, and compare the solution or project names directly instead of relying on window titles (which may be hacked by third party software as well). But using moniker it will only work if they are launched with the same privilege.
                            var currentInstanceName = Path.GetFileNameWithoutExtension(Globals.DTE.Solution.FullName);
                            if (string.IsNullOrEmpty(currentInstanceName) || ((from vsInstance in vsInstances
                                      where vsInstance.Id != currentInstance.Id
                                      select this.GetVSSolutionName(vsInstance.MainWindowTitle)).Any(vsInstanceName => vsInstanceName != null && currentInstanceName == vsInstanceName))) {
                                rewrite = true;
                            }
                        }
                    }
                }
                string pattern;
                var solution = Globals.DTE.Solution;
                if (solution == null || solution.FullName == string.Empty) {
                    var document = Globals.DTE.ActiveDocument;
                    var window = Globals.DTE.ActiveWindow;
                    if ((document == null || string.IsNullOrEmpty(document.FullName)) && (window == null || string.IsNullOrEmpty(window.Caption))) {
                        pattern = Convert.ToString(rewrite ? this.Settings.PatternIfNothingOpen : "[ideName]");
                    }
                    else {
                        pattern = Convert.ToString(rewrite ? this.Settings.PatternIfDocumentButNoSolutionOpen : "[documentName] - [ideName]");
                    }
                }
                else {
                    if (Globals.DTE.Debugger == null || Globals.DTE.Debugger.CurrentMode == dbgDebugMode.dbgDesignMode) {
                        pattern = Convert.ToString(rewrite ? this.Settings.PatternIfDesignMode : "[solutionName] - [ideName]");
                    }
                    else if (Globals.DTE.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode) {
                        pattern = Convert.ToString(rewrite ? this.Settings.PatternIfBreakMode : "[solutionName] (Debugging) - [ideName]");
                    }
                    else if (Globals.DTE.Debugger.CurrentMode == dbgDebugMode.dbgRunMode) {
                        pattern = Convert.ToString(rewrite ? this.Settings.PatternIfRunningMode : "[solutionName] (Running) - [ideName]");
                    }
                    else {
                        throw (new Exception("No matching state found"));
                    }
                }
                this.ChangeWindowTitle(this.GetNewTitle(pattern: pattern));
            }
            catch (Exception ex) {
                try {
                    if (this.Settings.EnableDebugMode) {
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

        private string GetNewTitle(string pattern) {
            var solution = Globals.DTE.Solution;
            var activeDocument = Globals.DTE.ActiveDocument;
            var activeWindow = Globals.DTE.ActiveWindow;
            if (activeDocument == null && (solution == null || string.IsNullOrEmpty(solution.FullName))) {
                var window = Globals.DTE.ActiveWindow;
                if (window == null || window.Caption == Globals.DTE.MainWindow.Caption) {
                    return this.IDEName;
                }
            }
            var parentPath = "";
            if (solution != null && !string.IsNullOrEmpty(solution.FullName)) {
                var parents = Path.GetDirectoryName(solution.FullName).Split(Path.DirectorySeparatorChar).Reverse().ToArray();
                parentPath = this.GetParentPath(parents: parents);
                pattern = this.ReplaceParentTags(pattern: pattern, parents: parents);
            }
            else if (activeDocument != null) {
                var parents = Path.GetDirectoryName(activeDocument.FullName).Split(Path.DirectorySeparatorChar).Reverse().ToArray();
                parentPath = this.GetParentPath(parents: parents);
                pattern = this.ReplaceParentTags(pattern: pattern, parents: parents);
            }
            pattern = this.ReplaceTag(pattern: pattern, tag: "[configurationName]", tagValueSelector: () => Globals.GetActiveConfigurationNameOrEmpty(solution), defaultValue: "");
            pattern = this.ReplaceTag(pattern: pattern, tag: "[platformName]", tagValueSelector: () => Globals.GetPlatformNameOrEmpty(solution), defaultValue: "");
            pattern = this.ReplaceTag(pattern: pattern, tag: "[projectName]", tagValueSelector: () => Globals.GetActiveProjectNameOrEmpty(), defaultValue: "");
            pattern = this.ReplaceTag(pattern: pattern, tag: "[solutionName]", tagValueSelector: () => Globals.GetSolutionNameOrEmpty(solution), defaultValue: "");
            pattern = this.ReplaceTag(pattern: pattern, tag: "[gitBranchName]", tagValueSelector: () => Globals.GetGitBranchNameOrEmpty(solution), defaultValue: "");
            pattern = this.ReplaceTag(pattern: pattern, tag: "[workspaceName]", tagValueSelector: () => Globals.GetWorkspaceNameOrEmpty(solution), defaultValue: "");
            pattern = this.ReplaceTag(pattern: pattern, tag: "[workspaceOwnerName]", tagValueSelector: () => Globals.GetWorkspaceOwnerNameOrEmpty(solution), defaultValue: "");
            pattern = this.ReplaceTag(pattern: pattern, tag: "[documentName]", tagValueSelector: () => Globals.GetActiveDocumentNameOrEmpty(activeDocument, activeWindow), defaultValue: "");
            pattern = this.ReplaceTag(pattern: pattern, tag: "[vsMajorVersion]", tagValueSelector: () => Globals.VsMajorVersion.ToString(CultureInfo.InvariantCulture), defaultValue: "");
            pattern = this.ReplaceTag(pattern: pattern, tag: "[vsMajorVersionYear]", tagValueSelector: () => Globals.VsMajorVersionYear.ToString(CultureInfo.InvariantCulture), defaultValue: "");
            pattern = this.ReplaceTag(pattern: pattern, tag: "[parentPath]", tagValueSelector: () => parentPath, defaultValue: "");
            pattern = this.ReplaceTag(pattern: pattern, tag: "[ideName]", tagValueSelector: () => this.IDEName, defaultValue: "");
            return pattern + " " + this.Settings.AppendedString;
        }

        private string ReplaceTag(string pattern, string tag, Func<string> tagValueSelector, string defaultValue) {
            if (!pattern.Contains(tag)) {
                return pattern.Replace(tag, defaultValue);
            }
            try {
                return pattern.Replace(tag, tagValueSelector() ?? defaultValue);
            }
            catch (Exception ex) {
                try {
                    if (this.Settings.EnableDebugMode) {
                        WriteOutput("ReplaceTag (" + tag + ") failed: " + ex);
                    }
                }
                catch {
                    // ignored
                }
                return pattern.Replace(tag, defaultValue);
            }
        }

        private string GetParentPath(string[] parents) {
            //TODO: handle drive letter better if (path1.Substring(path1.Length - 1, 1) == ":") path1 += System.IO.Path.DirectorySeparatorChar; http://stackoverflow.com/questions/1527942/why-path-combine-doesnt-add-the-path-directoryseparatorchar-after-the-drive-des?rq=1
            return Path.Combine(parents.Skip(this.Settings.ClosestParentDepth - 1).Take(this.Settings.FarthestParentDepth - this.Settings.ClosestParentDepth + 1).Reverse().ToArray());
        }

        private string ReplaceParentTags(string pattern, string[] parents) {
            var matches = new Regex("\\[parent([0-9]+)\\]").Matches(pattern);
            foreach (Match m in matches) {
                if (!m.Success) {
                    continue;
                }
                var depth = int.Parse(m.Groups[1].Captures[0].Value);
                if (depth <= parents.Length) {
                    pattern = pattern.Replace("[parent" + depth.ToString(CultureInfo.InvariantCulture) + "]", parents[depth]);
                }
            }
            return pattern;
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
                if (this.Settings.EnableDebugMode) {
                    WriteOutput("ChangeWindowTitle failed: " + ex);
                }
            }
        }

        private static void WriteOutput(string str) {
            try {
                Globals.InvokeOnUIThread(() => {
                    var outWindow = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                    var generalPaneGuid = VSConstants.OutputWindowPaneGuid.DebugPane_guid;
                    // P.S. There's also the VSConstants.GUID_OutWindowDebugPane available.
                    if (outWindow != null) {
                        IVsOutputWindowPane generalPane;
                        outWindow.GetPane(ref generalPaneGuid, out generalPane);
                        generalPane.OutputString("RenameVSWindowTitle: " + str + "\r\n");
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