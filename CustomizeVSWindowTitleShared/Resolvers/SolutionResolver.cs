using System;
using System.IO;
using System.Reflection;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ErwinMayerLabs.RenameVSWindowTitle.Resolvers {
    public class SolutionNameResolver : SimpleTagResolver {
        public SolutionNameResolver() : base(tagName: "solutionName") { }

        public override string Resolve(AvailableInfo info) {
            return info.Cfg.SolutionName ?? string.Empty;
        }

        /// <summary>
        /// Returns the full path of the solution filter (.slnf) the current solution was opened from,
        /// or an empty string when no filter is open (i.e. a plain solution or folder was opened).
        /// </summary>
        /// <remarks>
        /// Visual Studio does not expose a public API for this: when a .slnf is opened, DTE.Solution.FullName
        /// (and IVsSolution.GetSolutionInfo) return the underlying solution instead. The solution root
        /// hierarchy object (Microsoft.VisualStudio.CommonIDE.Solutions.Solution) carries an internal
        /// 'SolutionFilterFilePath' property that holds the filter path, so we read it via reflection and
        /// fall back gracefully (empty string) if that internal shape ever changes.
        /// Must be called on the UI thread.
        /// </remarks>
        public static string GetOpenSolutionFilterPathOrEmpty() {
            try {
                if (!(Package.GetGlobalService(typeof(SVsSolution)) is IVsSolution solutionService)) {
                    return string.Empty;
                }
                // The solution service object and its IVsHierarchy view are normally the same managed
                // instance; try both in case a future VS release splits them.
                foreach (var candidate in new object[] { solutionService, solutionService as IVsHierarchy }) {
                    if (candidate == null) {
                        continue;
                    }
                    var prop = candidate.GetType().GetProperty("SolutionFilterFilePath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null && prop.GetValue(candidate) is string filterPath && !string.IsNullOrEmpty(filterPath)) {
                        return filterPath;
                    }
                }
            }
            catch {
                // Internal API unavailable; caller falls back to the solution name.
            }
            return string.Empty;
        }

        public static bool IsOpenFolderSolution(string solutionFullName) {
            return !string.IsNullOrWhiteSpace(solutionFullName) && File.GetAttributes(solutionFullName).HasFlag(FileAttributes.Directory);
        }

        public static string GetSolutionNameOrEmpty(Solution solution) {
            return GetSolutionNameOrEmpty(solution?.FullName);
        }

        public static string GetSolutionNameOrEmpty(string solutionFullName) {
            if (string.IsNullOrEmpty(solutionFullName)) {
                return string.Empty;
            }
            if (IsOpenFolderSolution(solutionFullName)) {
                return new DirectoryInfo(solutionFullName).Name;
            }
            return Path.GetFileNameWithoutExtension(solutionFullName);
        }

        public static string GetSolutionFolderPathOrEmpty(Solution solution) {
            var sn = solution?.FullName;
            if (string.IsNullOrEmpty(sn)) {
                return string.Empty;
            }
            if (IsOpenFolderSolution(sn)) {
                return sn;
            }
            return new FileInfo(sn).DirectoryName;
        }
    }

    public class SolutionFilterNameResolver : SimpleTagResolver {
        public SolutionFilterNameResolver() : base(tagName: "solutionFilterName") { }

        public override string Resolve(AvailableInfo info) {
            // When a solution filter (.slnf) is open, show its name; otherwise fall back to the
            // solution name so the tag can be used in place of [solutionName] in every case.
            var filterName = CustomizeVSWindowTitle.CurrentPackage?.SolutionFilterName;
            if (!string.IsNullOrEmpty(filterName)) {
                return filterName;
            }
            return info.Cfg.SolutionName ?? string.Empty;
        }
    }
}
