using System;
using System.Linq;
using EnvDTE;
using System.Collections.Generic;
using ErwinMayerLabs.RenameVSWindowTitle.Resolvers;

namespace ErwinMayerLabs.RenameVSWindowTitle {
    public class AvailableInfo {
        private AvailableInfo() { }

        public string IdeName { get; private set; }
        public Solution Solution { get; private set; }
        public GlobalSettingsPageGrid GlobalSettings { get; private set; }
        public SettingsSet Cfg { get; private set; }
        public Document ActiveDocument { get; private set; }
        public Window ActiveWindow { get; private set; }
        public string DocumentName { get; private set; }
        public string Path { get; private set; }
        public string[] PathParts { get; private set; }
        public string[] DocumentPathParts { get; private set; }
        public string DocumentPath { get; private set; }
        public string WindowName { get; private set; }
        public string ElevationSuffix { get; private set; }

        public static AvailableInfo GetCurrent(string ideName, Solution solution, SettingsSet cfg, GlobalSettingsPageGrid globalSettings) {
            var info = new AvailableInfo {
                IdeName = ideName,
                Solution = solution,
                GlobalSettings = globalSettings,
                Cfg = cfg,
                ElevationSuffix = CustomizeVSWindowTitle.CurrentPackage.ElevationSuffix
            };
            try {
                info.ActiveDocument = Globals.DTE.ActiveDocument;
            }
            catch {
                // Do nothing
            }
            try {
                info.ActiveWindow = Globals.DTE.ActiveWindow;
            }
            catch {
                // Do nothing
            }
            var solutionFullName = info.Solution?.FullName;
            if (info.ActiveDocument == null && string.IsNullOrEmpty(solutionFullName)) {
                if (info.ActiveWindow == null || info.ActiveWindow.Caption == Globals.DTE.MainWindow.Caption) {
                    return null;
                }
            }
            info.DocumentName = DocumentHelper.GetActiveDocumentNameOrEmpty(info.ActiveDocument);
            info.DocumentPath = DocumentHelper.GetActiveDocumentPathOrEmpty(info.ActiveDocument);
            info.WindowName = DocumentHelper.GetActiveWindowNameOrEmpty(info.ActiveWindow);

            if (!string.IsNullOrEmpty(solutionFullName)) {
                info.Path = solutionFullName;
            }
            else {
                info.Path = info.DocumentPath;
            }

            info.PathParts = SplitPath(info.Path);
            if (!string.IsNullOrEmpty(info.Path)) {
                info.PathParts[0] = System.IO.Path.GetPathRoot(info.Path).Replace("\\", "");
            }

            info.DocumentPathParts = SplitPath(info.DocumentPath);
            if (!string.IsNullOrEmpty(info.DocumentPath)) {
                info.DocumentPathParts[0] = System.IO.Path.GetPathRoot(info.DocumentPath).Replace("\\", "");
            }
            return info;
        }

        private static string[] SplitPath(string path) {
            if (string.IsNullOrEmpty(path)) {
                return new string[0];
            }

            var root = System.IO.Path.GetPathRoot(path);
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(root)) {
                parts.Add(root);
            }
            parts.AddRange(path.Substring(root.Length).Split(new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries));
            return parts.ToArray();
        }

        public static string GetParentPath(string[] pathParts, int closestParentDepth, int farthestParentDepth) {
            if (closestParentDepth > farthestParentDepth) {
                // swap if provided in wrong order
                var t = closestParentDepth;
                closestParentDepth = farthestParentDepth;
                farthestParentDepth = t;
            }
            pathParts = pathParts.Reverse().Skip(closestParentDepth)
                                 .Take(farthestParentDepth - closestParentDepth + 1)
                                 .Reverse()
                                 .ToArray();
            if (pathParts.Length >= 2 && pathParts[0].EndsWith(":", StringComparison.Ordinal)) {
                pathParts[0] += System.IO.Path.DirectorySeparatorChar;
            }
            return System.IO.Path.Combine(pathParts);
        }
    }
}