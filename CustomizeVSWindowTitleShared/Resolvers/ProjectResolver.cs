using System;
using EnvDTE;
using EnvDTE80;

namespace ErwinMayerLabs.RenameVSWindowTitle.Resolvers {
    public static class ProjectHelper {
        public static bool TryGetActiveProject(DTE2 dte, out Project activeProject) {
            activeProject = null;
            try {
                // Get the project of the current displayed document
                var activeDocument = dte.ActiveDocument;
                if (activeDocument != null && activeDocument.ProjectItem != null && activeDocument.ProjectItem.ContainingProject != null) {
                    activeProject = activeDocument.ProjectItem.ContainingProject;
                    return true;
                }

                var activeSolutionProjects = dte.ActiveSolutionProjects as Array;
                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0) {
                    activeProject = activeSolutionProjects.GetValue(0) as Project;
                    return true;
                }
            }
            catch {
                // ignored
            }
            return false;
        }
    }

    public class ProjectNameResolver : SimpleTagResolver {
        public ProjectNameResolver() : base(tagName: "projectName") { }

        public override string Resolve(AvailableInfo info) {
            return GetActiveProjectNameOrEmpty();
        }

        public static string GetActiveProjectNameOrEmpty() {
            return ProjectHelper.TryGetActiveProject(Globals.DTE, out Project project) ? project.Name ?? string.Empty : "";
        }
    }
}