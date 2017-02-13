using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;

namespace ErwinMayerLabs.RenameVSWindowTitle {
    public static class Globals {
        public static DTE2 DTE;

        public const string SolutionSettingsOverrideExtension = ".rn.xml";
        public const string PathTag = "Path";
        public const string SolutionNameTag = "SolutionName";
        public const string ClosestParentDepthTag = "ClosestParentDepth";
        public const string FarthestParentDepthTag = "FarthestParentDepth";
        public const string AppendedStringTag = "AppendedString";
        public const string ElevatedStringTag = "ElevatedString";
        public const string PatternIfRunningModeTag = "PatternIfRunningMode";
        public const string PatternIfBreakModeTag = "PatternIfBreakMode";
        public const string PatternIfDesignModeTag = "PatternIfDesignMode";

        public static readonly Lazy<int> VsProcessId = new Lazy<int>(() => {
            using (var process = System.Diagnostics.Process.GetCurrentProcess()) {
                return process.Id;
            }
        });

        private static int? _VsMajorVersion;
        public static int VsMajorVersion {
            get {
                if (!_VsMajorVersion.HasValue) {
                    Version v;
                    _VsMajorVersion = Version.TryParse(DTE.Version, out v) ? v.Major : 10;
                }
                return _VsMajorVersion.Value;
            }
        }

        private static int? _VsMajorVersionYear;
        public static int VsMajorVersionYear => _VsMajorVersionYear ?? (_VsMajorVersionYear = GetYearFromVsMajorVersion(VsMajorVersion)).Value;

        public static string GetSolutionNameOrEmpty(Solution solution) {
            var sn = solution?.FullName;
            return string.IsNullOrEmpty(sn) ? "" : Path.GetFileNameWithoutExtension(sn);
        }

        public static string GetActiveProjectNameOrEmpty() {
            Project project;
            return TryGetActiveProject(DTE, out project) ? project.Name ?? string.Empty : "";
        }

        public static string GetActiveDocumentProjectNameOrEmpty(Document activeDocument) {
            return activeDocument?.ProjectItem?.ContainingProject?.Name ?? string.Empty;
        }

        public static string GetActiveDocumentProjectFileNameOrEmpty(Document activeDocument) {
            var fn = activeDocument?.ProjectItem?.ContainingProject?.FullName;
            return fn != null ? Path.GetFileName(fn) : string.Empty;
        }

        public static string GetActiveDocumentNameOrEmpty(Document activeDocument) {
            return activeDocument != null ? Path.GetFileName(activeDocument.FullName) : string.Empty;
        }

        public static string GetActiveDocumentPathOrEmpty(Document activeDocument) {
            return activeDocument != null ? activeDocument.FullName : string.Empty;
        }

        public static string GetActiveWindowNameOrEmpty(Window activeWindow) {
            if (activeWindow != null && activeWindow.Caption != DTE.MainWindow.Caption) {
                return activeWindow.Caption ?? string.Empty;
            }
            return string.Empty;
        }

        public static string GetActiveConfigurationNameOrEmpty(Solution solution) {
            if (string.IsNullOrEmpty(solution?.FullName)) return string.Empty;
            var activeConfig = (SolutionConfiguration2)solution.SolutionBuild.ActiveConfiguration;
            return activeConfig != null ? activeConfig.Name ?? string.Empty : string.Empty;
        }

        public static string GetPlatformNameOrEmpty(Solution solution) {
            if (string.IsNullOrEmpty(solution?.FullName)) return string.Empty;
            var activeConfig = (SolutionConfiguration2)solution.SolutionBuild.ActiveConfiguration;
            return activeConfig != null ? activeConfig.PlatformName ?? string.Empty : string.Empty;
        }

        public static string GetGitBranchNameOrEmpty(Solution solution) {
            var sn = solution?.FullName;
            if (string.IsNullOrEmpty(sn)) return string.Empty;
            var workingDirectory = new FileInfo(sn).DirectoryName;
            return IsGitRepository(workingDirectory) ? GetGitBranch(workingDirectory) ?? string.Empty : string.Empty;
        }

        public static string GetHgBranchNameOrEmpty(Solution solution) {
            var sn = solution?.FullName;
            if (string.IsNullOrEmpty(sn)) return string.Empty;
            var workingDirectory = new FileInfo(sn).DirectoryName;
            return IsHgRepository(workingDirectory) ? GetHgBranch(workingDirectory) ?? string.Empty : string.Empty;
        }

        public static string GetWorkspaceNameOrEmpty(Solution solution) {
            //dynamic vce = Globals.DTE.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt");
            //if (vce != null && vce.SolutionWorkspace != null) {
            //    return vce.SolutionWorkspace.Name;
            //}  
            var sn = solution?.FullName;
            if (string.IsNullOrEmpty(sn)) return string.Empty;
            var name = string.Empty;
            InvokeOnUIThread(() => name = WorkspaceInfoGetter.Instance().GetName(sn));
            return name ?? string.Empty;
        }

        public static string GetWorkspaceOwnerNameOrEmpty(Solution solution) {
            //dynamic vce = Globals.DTE.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt");
            //if (vce != null && vce.SolutionWorkspace != null) {
            //    return vce.SolutionWorkspace.OwnerName;
            //}  
            var sn = solution?.FullName;
            if (string.IsNullOrEmpty(sn)) return string.Empty;
            var name = string.Empty;
            InvokeOnUIThread(() => name = WorkspaceInfoGetter.Instance().GetOwner(sn));
            return name ?? string.Empty;
        }

        public static string GetExampleSolution(string solutionPath) {
            return string.IsNullOrEmpty(solutionPath) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"SampleDir\SampleDir2\SampleDir3\SampleDir4\Sample.sln") : solutionPath;
        }

        public static string GetHgBranch(string workingDirectory) {
            using (var pProcess = new System.Diagnostics.Process {
                StartInfo = {
                    FileName = HgExecFp,
                    Arguments = "branch",
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            }) {
                pProcess.Start();
                var branchName = pProcess.StandardOutput.ReadToEnd().TrimEnd(' ', '\r', '\n');
                pProcess.WaitForExit();
                return branchName;
            }
        }

        public static string GetGitBranch(string workingDirectory) {
            using (var pProcess = new System.Diagnostics.Process {
                StartInfo = {
                    FileName = GitExecFp,
                    Arguments = "symbolic-ref --short -q HEAD", //As per: http://git-blame.blogspot.sg/2013/06/checking-current-branch-programatically.html. Or: "rev-parse --abbrev-ref HEAD"
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            }) {
                pProcess.Start();
                var branchName = pProcess.StandardOutput.ReadToEnd().TrimEnd(' ', '\r', '\n');
                pProcess.WaitForExit();
                return branchName;
            }
        }

        public const string HgExecFn = "hg.exe";
        private static string HgExecFp = HgExecFn;

        public static void UpdateHgExecFp(string hgDp) {
            if (string.IsNullOrEmpty(hgDp)) {
                HgExecFp = HgExecFn;
                return;
            }
            HgExecFp = Path.Combine(hgDp, HgExecFn);
        }

        public const string GitExecFn = "git.exe";
        private static string GitExecFp = GitExecFn;

        public static void UpdateGitExecFp(string gitDp) {
            if (string.IsNullOrEmpty(gitDp)) {
                GitExecFp = GitExecFn;
                return;
            }
            GitExecFp = Path.Combine(gitDp, GitExecFn);
        }

        public static bool IsGitRepository(string workingDirectory) {
            using (var pProcess = new System.Diagnostics.Process {
                StartInfo = {
                    FileName = GitExecFp,
                    Arguments = "rev-parse --is-inside-work-tree",
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            }) {
                pProcess.Start();
                var res = pProcess.StandardOutput.ReadToEnd().TrimEnd(' ', '\r', '\n');
                pProcess.WaitForExit();
                return res == "true";
            }
        }

        public static bool IsHgRepository(string workingDirectory) {
            using (var pProcess = new System.Diagnostics.Process {
                StartInfo = {
                    FileName = HgExecFp,
                    Arguments = "root",
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    //RedirectStandardError = true, var error = pProcess.StandardError.ReadToEnd();
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            }) {
                pProcess.Start();
                var res = pProcess.StandardOutput.ReadToEnd().TrimEnd('\r', '\n', Path.DirectorySeparatorChar);
                pProcess.WaitForExit();
                return !string.IsNullOrWhiteSpace(res) && workingDirectory.TrimEnd(Path.DirectorySeparatorChar).StartsWith(res);
            }
        }

        public static bool TryGetActiveProject(DTE2 dte, out Project activeProject) {
            activeProject = null;
            try {
                if (dte.ActiveSolutionProjects != null) {
                    var activeSolutionProjects = dte.ActiveSolutionProjects as Array;
                    if (activeSolutionProjects != null && activeSolutionProjects.Length > 0) {
                        activeProject = activeSolutionProjects.GetValue(0) as Project;
                        return true;
                    }
                }
            }
            catch {
                // ignored
            }
            return false;
        }

        public static void InvokeOnUIThread(Action action) {
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            dispatcher?.Invoke(action);
        }

        public static void BeginInvokeOnUIThread(Action action) {
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            dispatcher?.BeginInvoke(action);
        }

        [DllImport("ole32.dll")]
        static extern int GetRunningObjectTable(uint reserved, out IRunningObjectTable pprot);
        [DllImport("ole32.dll")]
        static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        static readonly Regex m_DTEComObjectNameRegex = new Regex(@"^!VisualStudio\.DTE\.(?<dte_version>\d+\.\d+).*$", RegexOptions.Compiled);

        public struct VSMultiInstanceInfo {
            public bool multiple_instances;
            public bool multiple_instances_same_version;
            public int nb_instances_same_solution;
        }

        public static void GetVSMultiInstanceInfo(out VSMultiInstanceInfo vs_instance_info) {
            GetVSMultiInstanceInfo(out vs_instance_info, DTE.Version, DTE.Solution);
        }

        public static void GetVSMultiInstanceInfo(out VSMultiInstanceInfo vs_instance_info, string our_dte_version, Solution solution) {
            vs_instance_info.multiple_instances = false;
            vs_instance_info.multiple_instances_same_version = false;
            vs_instance_info.nb_instances_same_solution = 0;
            try {
                IRunningObjectTable running_object_table;
                if (VSConstants.S_OK != GetRunningObjectTable(0, out running_object_table))
                    return;
                IEnumMoniker moniker_enumerator;
                running_object_table.EnumRunning(out moniker_enumerator);
                moniker_enumerator.Reset();

                var monikers = new IMoniker[1];
                var num_fetched = IntPtr.Zero;
                int dte_count = 0;
                int dte_count_our_version = 0;
                //Will only return if same privilege as per http://stackoverflow.com/questions/11835617/understanding-the-running-object-table 
                while (VSConstants.S_OK == moniker_enumerator.Next(1, monikers, num_fetched)) {
                    IBindCtx ctx;
                    if (VSConstants.S_OK != CreateBindCtx(0, out ctx))
                        continue;

                    string name;
                    monikers[0].GetDisplayName(ctx, null, out name);
                    if (!name.StartsWith("!VisualStudio.DTE."))
                        continue;

                    object com_object;
                    if (VSConstants.S_OK != running_object_table.GetObject(monikers[0], out com_object))
                        continue;

                    var dte = com_object as DTE2;
                    if (dte != null) {
                        var s = dte.Solution;
                        if (s != null) {
                            var sn = Path.GetFileNameWithoutExtension(s.FullName);
                            if (!string.IsNullOrEmpty(sn) && solution != null && sn == Path.GetFileNameWithoutExtension(solution.FullName)) {
                                vs_instance_info.nb_instances_same_solution++;
                            }
                        }
                        ++dte_count;
                        var m = m_DTEComObjectNameRegex.Match(name);
                        if (m.Success) {
                            var g = m.Groups["dte_version"];
                            if (g.Success && g.Value == our_dte_version)
                                ++dte_count_our_version;
                        }
                    }
                }
                vs_instance_info.multiple_instances = dte_count > 1;
                vs_instance_info.multiple_instances_same_version = dte_count_our_version > 1;
            }
            catch {
                vs_instance_info.multiple_instances = false;
                vs_instance_info.multiple_instances_same_version = false;
            }
        }

        private static int GetYearFromVsMajorVersion(int version) {
            switch (version) {
                case 9:
                    return 2008;
                case 10:
                    return 2010;
                case 11:
                    return 2012;
                case 12:
                    return 2013;
                case 14:
                    return 2015;
                default:
                    return version;
            }
        }
    }
}
