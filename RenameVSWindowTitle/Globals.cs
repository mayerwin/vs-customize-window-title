using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;

namespace ErwinMayerLabs.RenameVSWindowTitle {
    public static class Globals {
        public static DTE2 DTE;

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
        public static int VsMajorVersionYear {
            get {
                return _VsMajorVersionYear ?? (_VsMajorVersionYear = GetYearFromVsMajorVersion(VsMajorVersion)).Value;
            }
        }

        public static string GetSolutionNameOrEmpty(Solution solution) {
            return solution == null || string.IsNullOrEmpty(solution.FullName) ? "" : Path.GetFileNameWithoutExtension(solution.FullName);
        }

        public static string GetActiveProjectNameOrEmpty() {
            Project project;
            return TryGetActiveProject(DTE, out project) ? project.Name : "";
        }

        public static string GetActiveDocumentNameOrEmpty(Document activeDocument, Window activeWindow) {
            if (activeDocument != null) {
                return Path.GetFileName(activeDocument.FullName);
            }
            if (activeWindow != null && activeWindow.Caption != DTE.MainWindow.Caption) {
                return activeWindow.Caption;
            }
            return "";
        }

        public static string GetActiveConfigurationNameOrEmpty(Solution solution) {
            if (solution == null || string.IsNullOrEmpty(solution.FullName)) return "";
            var activeConfig = (SolutionConfiguration2)solution.SolutionBuild.ActiveConfiguration;
            return activeConfig != null ? activeConfig.Name : "";
        }

        public static string GetPlatformNameOrEmpty(Solution solution) {
            if (solution == null || string.IsNullOrEmpty(solution.FullName)) return "";
            var activeConfig = (SolutionConfiguration2)solution.SolutionBuild.ActiveConfiguration;
            return activeConfig != null ? activeConfig.PlatformName : "";
        }

        public static string GetGitBranchNameOrEmpty(Solution solution) {
            if (solution == null || string.IsNullOrEmpty(solution.FullName)) return "";
            var workingDirectory = new FileInfo(solution.FullName).DirectoryName;
            return IsGitRepository(workingDirectory) ? GetGitBranch(workingDirectory) : "";
        }

        public static string GetWorkspaceNameOrEmpty(Solution solution) {
            //dynamic vce = Globals.DTE.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt");
            //if (vce != null && vce.SolutionWorkspace != null) {
            //    return vce.SolutionWorkspace.Name;
            //}  
            if (solution == null || string.IsNullOrEmpty(solution.FullName)) return "";
            var name = "";
            InvokeOnUIThread(() => name = WorkspaceInfoGetter.Instance().GetName(solution.FullName));
            return name;
        }

        public static string GetWorkspaceOwnerNameOrEmpty(Solution solution) {
            //dynamic vce = Globals.DTE.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt");
            //if (vce != null && vce.SolutionWorkspace != null) {
            //    return vce.SolutionWorkspace.OwnerName;
            //}  
            if (solution == null || string.IsNullOrEmpty(solution.FullName)) return "";
            var name = "";
            InvokeOnUIThread(() => name = WorkspaceInfoGetter.Instance().GetOwner(solution.FullName));
            return name;
        }

        public static string GetGitBranch(string workingDirectory) {
            //Create process
            var pProcess = new System.Diagnostics.Process {
                StartInfo = {
                    FileName = "git.exe",
                    Arguments = "symbolic-ref --short -q HEAD", //As per: http://git-blame.blogspot.sg/2013/06/checking-current-branch-programatically.html. Or: "rev-parse --abbrev-ref HEAD"
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            };

            //Start the process
            pProcess.Start();

            //Get program output
            var branchName = pProcess.StandardOutput.ReadToEnd().TrimEnd(' ', '\r', '\n');

            //Wait for process to finish
            pProcess.WaitForExit();

            return branchName;
        }

        public static bool IsGitRepository(string workingDirectory) {
            //Create process
            var pProcess = new System.Diagnostics.Process {
                StartInfo = {
                    FileName = "git.exe",
                    Arguments = "rev-parse --is-inside-work-tree",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            };

            //Start the process
            pProcess.Start();

            //Get program output
            var res = pProcess.StandardOutput.ReadToEnd().TrimEnd(' ', '\r', '\n');

            //Wait for process to finish
            pProcess.WaitForExit();

            return res == "true";
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
            if (dispatcher != null) {
                dispatcher.Invoke(action);
            }
        }

        public static void BeginInvokeOnUIThread(Action action) {
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            if (dispatcher != null) {
                dispatcher.BeginInvoke(action);
            }
        }

        [DllImport("ole32.dll")]
        static extern int GetRunningObjectTable(uint reserved, out IRunningObjectTable pprot);
        [DllImport("ole32.dll")]
        static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        static readonly Regex m_DTEComObjectNameRegex = new Regex(@"^!VisualStudio\.DTE\.(?<dte_version>\d+\.\d+).*$");

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
