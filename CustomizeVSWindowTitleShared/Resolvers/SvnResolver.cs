using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE;

using ErwinMayerLabs.Lib;

namespace ErwinMayerLabs.RenameVSWindowTitle.Resolvers {
    public class SvnResolver : TagResolver, ISimpleTagResolver {
        public const string SvnExecFn = "svn.exe";
        private static string SvnExecFp = SvnExecFn;
        private const string tagName = "svnDirectoryName";

        public SvnResolver() : base(tagNames: new[] { tagName, tagName + ":X", tagName + ":X:Y"}) { }

        public string TagName { get; set; } = tagName;

        public string Resolve(AvailableInfo info) {
            UpdateSvnExecFp(info.GlobalSettings.SvnDirectory);
            return GetSvnDirectoryOrEmpty(info.Solution);
        }

        public override bool TryResolve(string tag, AvailableInfo info, out string s) {
            s = null;
            if (!tag.StartsWith(tagName, StringComparison.InvariantCulture)) return false;
            var svnPath = this.Resolve(info);
            if (string.IsNullOrWhiteSpace(svnPath)) return false;
            var directorySeparator = info.GlobalSettings.SvnDirectorySeparator;
            var svnPathParts = new List<string>();
            if (Path.IsPathRooted(svnPath)) svnPathParts.Add(directorySeparator);
            svnPathParts.AddRange(svnPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries));
            var m = Globals.RangeRegex.Match(tag.Substring(tagName.Length));
            if (m.Success) {
                if (!svnPathParts.Any()) {
                    s = string.Empty;
                }
                else {
                    var x = int.Parse(m.Groups["startIndex"].Value, CultureInfo.InvariantCulture);
                    var y = int.Parse(m.Groups["endIndex"].Value, CultureInfo.InvariantCulture);
                    var pathRange = Globals.GetPathRange(svnPathParts, x, y);
                    s = string.Join(directorySeparator, pathRange);
                }
                return true;
            }
            m = Globals.IndexRegex.Match(tag.Substring(tagName.Length));
            if (m.Success) {
                if (!svnPathParts.Any()) {
                    s = string.Empty;
                }
                else {
                    var index = int.Parse(m.Groups["index"].Value, CultureInfo.InvariantCulture);
                    s = Globals.GetPathPart(svnPathParts, index);
                }
                return true;
            }
            return false;
        }

        public static void UpdateSvnExecFp(string svnDp) {
            if (string.IsNullOrEmpty(svnDp)) {
                SvnExecFp = SvnExecFn;
                return;
            }
            SvnExecFp = Path.Combine(svnDp, SvnExecFn);
        }

        public static string GetSvnDirectoryOrEmpty(Solution solution) {
            var baseDir = SolutionNameResolver.GetSolutionFolderPathOrEmpty(solution);
            if (baseDir == string.Empty) {
                return string.Empty;
            }
            return GetSvnDirectory(baseDir) ?? string.Empty;
        }

        public static string GetSvnDirectory(string workingDirectory) {
            try {
                using (var pProcess = new System.Diagnostics.Process {
                    StartInfo = {
                        FileName = SvnExecFp,
                        Arguments = "info",
                        UseShellExecute = false,
                        StandardOutputEncoding = Encoding.UTF8,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        WorkingDirectory = workingDirectory
                    }
                }) {
                    pProcess.Start();
                    var output = pProcess.StandardOutput.ReadToEnd();
                    pProcess.WaitForExit();
                    if (pProcess.ExitCode != 0) {
                        // Not a working directory.
                        return null;
                    }
                    const string Prefix = "Relative URL: ";
                    var branchLine = output.Split('\n').SingleOrDefault(line => line.StartsWith(Prefix, StringComparison.Ordinal));
                    return branchLine?.Substring(Prefix.Length).Trim('^', '\n', '\r', ' ');
                }
            }
            catch (Exception ex) {
                try {
                    if (CustomizeVSWindowTitle.CurrentPackage.UiSettings.EnableDebugMode) {
                        CustomizeVSWindowTitle.WriteOutput("SvnResolver.GetSvnDirectory() exception: " + ex);
                    }
                }
                catch {
                    // ignored
                }
                return null;
            }
        }
    }
}