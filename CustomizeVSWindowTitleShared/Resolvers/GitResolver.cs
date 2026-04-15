using System.IO;
using System.Text;
using EnvDTE;

namespace ErwinMayerLabs.RenameVSWindowTitle.Resolvers {
    public class GitBranchNameResolver : SimpleTagResolver {
        public GitBranchNameResolver() : base(tagName: "gitBranchName") { }

        public override string Resolve(AvailableInfo info) {
            GitInfo.UpdateGitExecFp(info.GlobalSettings.GitDirectory); // there is likely a better way to adjust the git path
            return GetGitBranchNameOrEmpty(info, info.Solution);
        }

        public static string GetGitBranchNameOrEmpty(AvailableInfo info, Solution solution) {
            var baseDir = SolutionNameResolver.GetSolutionFolderPathOrEmpty(solution);
            if (baseDir == string.Empty) {
                return string.Empty;
            }
            var workingDirectory = GitInfo.ResolveGitWorkingDirectory(baseDir, info.Cfg?.GitWorkingDirectory);
            if (workingDirectory == null) return string.Empty;
            return GetGitBranch(workingDirectory) ?? string.Empty;
        }

        public static string GetGitBranch(string workingDirectory) {
            using (var pProcess = new System.Diagnostics.Process {
                StartInfo = {
                    FileName = GitInfo.GetGitExecFp(),
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
                if (pProcess.ExitCode != 0) return "detached HEAD";
                return branchName;
            }
        }
    }

    public class GitRepoNameResolver : SimpleTagResolver {
        public GitRepoNameResolver() : base(tagName: "gitRepositoryName") { }

        public override string Resolve(AvailableInfo info) {
            GitInfo.UpdateGitExecFp(info.GlobalSettings.GitDirectory); // there is likely a better way to adjust the git path
            return GetGitRepoNameOrEmpty(info, info.Solution);
        }

        public static string GetGitRepoNameOrEmpty(AvailableInfo info, Solution solution) {
            var baseDir = SolutionNameResolver.GetSolutionFolderPathOrEmpty(solution);
            if (baseDir == string.Empty) {
                return string.Empty;
            }
            var workingDirectory = GitInfo.ResolveGitWorkingDirectory(baseDir, info.Cfg?.GitWorkingDirectory);
            if (workingDirectory == null) return string.Empty;
            return GetGitRepoName(workingDirectory) ?? string.Empty;
        }

        public static string GetGitRepoName(string workingDirectory) {
            using (var pProcess = new System.Diagnostics.Process {
                StartInfo = {
                    FileName = GitInfo.GetGitExecFp(),
                    Arguments = "rev-parse --show-toplevel", //As per: https://stackoverflow.com/questions/15715825/how-do-you-get-the-git-repositorys-name-in-some-git-repository
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            }) {
                pProcess.Start();
                var rootDirectory = pProcess.StandardOutput.ReadToEnd().TrimEnd(' ', '\r', '\n');
                pProcess.WaitForExit();
                if (pProcess.ExitCode != 0) return string.Empty;
                return new DirectoryInfo(rootDirectory).Name;
            }
        }
    }

    public class GitInfo {
        public const string GitExecFn = "git.exe";
        private static string GitExecFp = GitExecFn;

        public static void UpdateGitExecFp(string gitDp) {
            if (string.IsNullOrEmpty(gitDp)) {
                GitExecFp = GitExecFn;
                return;
            }
            GitExecFp = Path.Combine(gitDp, GitExecFn);
        }

        public static string GetGitExecFp() {
            return GitExecFp;
        }

        /// <summary>
        /// Resolves the git working directory by trying (in order):
        /// 1. The solution directory itself.
        /// 2. The user-configured GitWorkingDirectory override from XML config (if set).
        /// Returns null if no git repository is found.
        /// </summary>
        public static string ResolveGitWorkingDirectory(string solutionDir, string gitWorkingDirectorySetting) {
            if (IsGitRepository(solutionDir)) return solutionDir;
            if (!string.IsNullOrEmpty(gitWorkingDirectorySetting) && IsGitRepository(gitWorkingDirectorySetting)) {
                return gitWorkingDirectorySetting;
            }
            return null;
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
    }
}