using System.IO;
using System.Text;
using EnvDTE;

namespace ErwinMayerLabs.RenameVSWindowTitle.Resolvers {
    public class HgBranchNameResolver : SimpleTagResolver {
        public HgBranchNameResolver() : base(tagName: "hgBranchName") { }

        public override string Resolve(AvailableInfo info) {
            UpdateHgExecFp(info.GlobalSettings.HgDirectory);
            return GetHgBranchNameOrEmpty(info.Solution);
        }

        public static string GetHgBranchNameOrEmpty(Solution solution) {
            var baseDir = SolutionNameResolver.GetSolutionFolderPathOrEmpty(solution);
            if (baseDir == string.Empty) {
                return string.Empty;
            }
            return IsHgRepository(baseDir) ? GetHgBranch(baseDir) ?? string.Empty : string.Empty;
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

        public const string HgExecFn = "hg.exe";
        private static string HgExecFp = HgExecFn;

        public static void UpdateHgExecFp(string hgDp) {
            if (string.IsNullOrEmpty(hgDp)) {
                HgExecFp = HgExecFn;
                return;
            }
            HgExecFp = Path.Combine(hgDp, HgExecFn);
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

    }
}