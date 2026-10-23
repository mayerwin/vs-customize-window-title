/* Requires references to these TFS and Visual Studio assemblies:
 *  Microsoft.TeamFoundation.Client
 *  Microsoft.TeamFoundation.Common
 *  Microsoft.TeamFoundation.VersionControl.Client
 *  Microsoft.TeamFoundation.VersionControl.Common
 *  Microsoft.VisualStudio.Services.Client.Interactive
 *  Microsoft.VisualStudio.Services.Common
 * You'll find them in the TF.exe path (for VS2017 community edition: Microsoft Visual Studio\2017\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer)
 * For each of these references, set "Specific version" and "Local copy" to false, because you want to use whatever comes with Visual Studio at runtime
 */

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;

namespace ErwinMayerLabs.RenameVSWindowTitle.Lib {
    internal static class TfsHelper {
        private static readonly TimeSpan BranchCacheDuration = TimeSpan.FromSeconds(30);
        private static readonly object SyncRoot = new object();
        //Fallback collections built from cached credentials, one per server. They never prompt, so they are cheap to keep.
        private static readonly Dictionary<string, TfsTeamProjectCollection> Collections = new Dictionary<string, TfsTeamProjectCollection>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Tuple<string, DateTime>> BranchCache = new Dictionary<string, Tuple<string, DateTime>>(StringComparer.OrdinalIgnoreCase);

        public static string GetBranchNameFromLocalFile(string path) {
            if (string.IsNullOrEmpty(path))
                return null;
            lock (SyncRoot) {
                if (BranchCache.TryGetValue(path, out var cached) && DateTime.UtcNow - cached.Item2 < BranchCacheDuration)
                    return cached.Item1;
                string branchName = null;
                try {
                    branchName = GetBranchNameFromLocalFileUncached(path);
                }
                catch (Exception) {
                    //Typically not authenticated (we never prompt): show nothing rather than breaking the whole title.
                }
                //Also cache failures, to avoid hitting the server on every title refresh.
                BranchCache[path] = Tuple.Create(branchName, DateTime.UtcNow);
                return branchName;
            }
        }

        private static string GetBranchNameFromLocalFileUncached(string path) {
            var url = GetRepositoryUrl(path);
            if (string.IsNullOrEmpty(url))
                return null;
            var col = GetVsTeamProjectCollection(url) ?? GetOrCreateTfsCollection(url);
            var tfs = GetVersionControlServer(col);
            var branch = FindBranch(tfs, path);
            return System.IO.Path.GetFileName(branch);
        }

        //The collection Visual Studio (Team Explorer) is connected to: already authenticated, with credentials managed by VS itself.
        private static TfsTeamProjectCollection GetVsTeamProjectCollection(string url) {
            TfsTeamProjectCollection col = null;
            Globals.InvokeOnUIThread(() => {
                var contextManager = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(ITeamFoundationContextManager)) as ITeamFoundationContextManager;
                col = contextManager?.CurrentContext?.TeamProjectCollection;
            });
            if (col == null || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;
            return Uri.Compare(col.Uri, uri, UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0 ? col : null;
        }

        private static TfsTeamProjectCollection GetOrCreateTfsCollection(string tfsName) {
            if (!Collections.TryGetValue(tfsName, out var col)) {
                col = CreateTfsCollection(tfsName);
                Collections[tfsName] = col;
            }
            return col;
        }

        public static string FindBranch(VersionControlServer tfs, string path, VersionSpec versionSpec = null) {
            if (versionSpec == null)
                versionSpec = VersionSpec.Latest;
            try {
                while (!string.IsNullOrEmpty(path)) {
                    var item = tfs.GetItem(path, versionSpec, DeletedState.NonDeleted, GetItemsOptions.IncludeBranchInfo);
                    if (item == null)
                        return null;
                    if (item.IsBranch)
                        return path;
                    path = System.IO.Path.GetDirectoryName(path);
                }
                return null;
            }
            catch (ItemNotMappedException) {
                return null;
            }
        }

        public static VersionControlServer GetVersionControlServer(TfsTeamProjectCollection collection) {
            return collection.GetService<VersionControlServer>();
        }

        public static TfsTeamProjectCollection CreateTfsCollection(string tfsName) {
            //Never prompts: the window title must not pop up login dialogs. Without valid cached credentials, server calls throw and the tag stays empty.
            //Works for VS 2017+, with assemblies from VS 15. Does not work when compiling the VSIX from VS 2015 (will cause an exception saying the assembly could not be found).
            Uri fullyQualifiedUriForName = TfsTeamProjectCollection.GetFullyQualifiedUriForName(tfsName);
            VssCredentials vssCredentials = null;
            try {
                vssCredentials = VssClientCredentials.LoadCachedCredentials(fullyQualifiedUriForName, false, CredentialPromptType.DoNotPrompt);
            }
            catch (Exception) {
            }
            if (vssCredentials == null) {
                vssCredentials = new VssClientCredentials(useDefaultCredentials: true) { PromptType = CredentialPromptType.DoNotPrompt };
            }
            return new TfsTeamProjectCollection(fullyQualifiedUriForName, vssCredentials);
        }

        public static string GetRepositoryUrl(string path) {
            string text = null;
            if (!VersionControlPath.IsServerItem(path)) {
                try {
                    text = GetWorkspace(path)?.ServerUri?.AbsoluteUri;
                }
                catch (ApplicationException) {
                }
            }
            return text;
        }

        private static WorkspaceInfo GetWorkspace(string path) {
            if (path != null) {
                try {
                    if (!VersionControlPath.IsServerItem(path) && Workstation.Current.IsMapped(path)) {
                        return Workstation.Current.GetLocalWorkspaceInfo(path);
                    }
                }
                catch (Microsoft.TeamFoundation.InvalidPathException) {
                }
            }
            return null;
        }

    }
}
