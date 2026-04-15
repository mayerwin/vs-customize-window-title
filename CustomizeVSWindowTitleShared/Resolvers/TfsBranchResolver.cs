using EnvDTE;
using System.IO;
using ErwinMayerLabs.RenameVSWindowTitle.Lib;

namespace ErwinMayerLabs.RenameVSWindowTitle.Resolvers {
    public class TfsBranchNameResolver : SimpleTagResolver {
        public TfsBranchNameResolver() : base(tagName: "tfsBranchName") { }

        public override string Resolve(AvailableInfo info) {
            return GetBranchNameOrEmpty(info.Solution);
        }

        public static string GetBranchNameOrEmpty(Solution solution) {
            //dynamic vce = Globals.DTE.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt");
            //if (vce != null && vce.SolutionWorkspace != null) {
            //    return vce.SolutionWorkspace.Name;
            //}  

            var baseDir = SolutionNameResolver.GetSolutionFolderPathOrEmpty(solution);
            if (baseDir == string.Empty) {
                return string.Empty;
            }
            //Globals.InvokeOnUIThread(() => name = TfsHelper.GetBranchNameFromLocalFile(baseDir)); //we could try first to use this name value if not empty, but causes slowness due to UI thread.
            return TfsHelper.GetBranchNameFromLocalFile(baseDir) ?? string.Empty;
        }
    }
}