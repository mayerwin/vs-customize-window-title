using System;
using System.IO;
using EnvDTE;

namespace ErwinMayerLabs.RenameVSWindowTitle.Resolvers {
    public class SolutionNameResolver : SimpleTagResolver {
        public SolutionNameResolver() : base(tagName: "solutionName") { }

        public override string Resolve(AvailableInfo info) {
            return info.Cfg.SolutionName ?? string.Empty;
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
}
