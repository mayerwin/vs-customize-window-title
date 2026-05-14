using System.IO;
using System.Text;
using EnvDTE;

namespace ErwinMayerLabs.RenameVSWindowTitle.Resolvers
{
    public static class HgHelper
    {
        public const string HgExecFn = "hg.exe";
        private static string HgExecFp = HgExecFn;

        public static void UpdateHgExecFp(string hgDp)
        {
            if (string.IsNullOrEmpty(hgDp))
            {
                HgExecFp = HgExecFn;
                return;
            }
            HgExecFp = Path.Combine(hgDp, HgExecFn);
        }

        public static bool IsHgRepository(string workingDirectory)
        {
            using (var pProcess = new System.Diagnostics.Process
            {
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
            })
            {
                pProcess.Start();
                var res = pProcess.StandardOutput.ReadToEnd().TrimEnd('\r', '\n', Path.DirectorySeparatorChar);
                pProcess.WaitForExit();
                return !string.IsNullOrWhiteSpace(res) && workingDirectory.TrimEnd(Path.DirectorySeparatorChar).StartsWith(res);
            }
        }

        public static string GetHgCommandOrEmpty(Solution solution, string command)
        {
            var baseDir = SolutionNameResolver.GetSolutionFolderPathOrEmpty(solution);
            if (baseDir == string.Empty)
            {
                return string.Empty;
            }
            return IsHgRepository(baseDir) ? GetHgCommand(baseDir, command) ?? string.Empty : string.Empty;
        }

        public static string GetHgCommand(string workingDirectory, string command)
        {
            using (var pProcess = new System.Diagnostics.Process
            {
                StartInfo = {
                    FileName = HgExecFp,
                    Arguments = command,
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            })
            {
                pProcess.Start();
                var branchName = pProcess.StandardOutput.ReadToEnd().TrimEnd(' ', '\r', '\n');
                pProcess.WaitForExit();
                return branchName;
            }
        }

    }

    public class HgBookmarkNameResolver : SimpleTagResolver
    {
        public HgBookmarkNameResolver() : base(tagName: "hgBookmarkName") { }

        public override string Resolve(AvailableInfo info)
        {
            HgHelper.UpdateHgExecFp(info.GlobalSettings.HgDirectory);
            return HgHelper.GetHgCommandOrEmpty(info.Solution, "bookmark -ql .");
        }
    }
    public class HgBranchNameResolver : SimpleTagResolver
    {

        public HgBranchNameResolver() : base(tagName: "hgBranchName") { }

        public override string Resolve(AvailableInfo info)
        {
            HgHelper.UpdateHgExecFp(info.GlobalSettings.HgDirectory);
            return HgHelper.GetHgCommandOrEmpty(info.Solution, "branch");
        }

    }
}