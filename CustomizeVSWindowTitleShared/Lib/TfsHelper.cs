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

namespace ErwinMayerLabs.RenameVSWindowTitle.Lib {
    internal static class TfsHelper {
        private static readonly TimeSpan BranchCacheDuration = TimeSpan.FromSeconds(30);
        private static readonly object SyncRoot = new object();
        //One authenticated collection per server, so that the user is prompted at most once per session (the title is refreshed every few seconds).
        private static readonly Dictionary<string, TfsTeamProjectCollection> Collections = new Dictionary<string, TfsTeamProjectCollection>(StringComparer.OrdinalIgnoreCase);
        //Servers for which authentication failed or was cancelled: do not prompt again until the next ResetFailures().
        private static readonly HashSet<string> FailedServers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Tuple<string, DateTime>> BranchCache = new Dictionary<string, Tuple<string, DateTime>>(StringComparer.OrdinalIgnoreCase);

        //Called on user-initiated events (solution opened/closed, settings changed), so that a cancelled login is retried then rather than on every title refresh.
        //Only sets a flag: taking SyncRoot here could deadlock the UI thread while a login prompt is shown from the background thread.
        public static void ResetFailures() {
            ResetRequested = true;
        }

        private static volatile bool ResetRequested;

        public static string GetBranchNameFromLocalFile(string path) {
            if (string.IsNullOrEmpty(path))
                return null;
            lock (SyncRoot) {
                if (ResetRequested) {
                    ResetRequested = false;
                    FailedServers.Clear();
                    BranchCache.Clear();
                }
                if (BranchCache.TryGetValue(path, out var cached) && DateTime.UtcNow - cached.Item2 < BranchCacheDuration)
                    return cached.Item1;
                string branchName = null;
                try {
                    branchName = GetBranchNameFromLocalFileUncached(path);
                    return branchName;
                }
                finally {
                    //Also cache failures, to avoid hitting the server on every title refresh.
                    BranchCache[path] = Tuple.Create(branchName, DateTime.UtcNow);
                }
            }
        }

        private static string GetBranchNameFromLocalFileUncached(string path) {
            var url = GetRepositoryUrl(path);
            if (string.IsNullOrEmpty(url))
                return null;
            var col = GetOrCreateTfsCollection(url);
            if (col == null)
                return null;
            var tfs = GetVersionControlServer(col);
            var branch = FindBranch(tfs, path);
            return System.IO.Path.GetFileName(branch);
        }

        private static TfsTeamProjectCollection GetOrCreateTfsCollection(string tfsName) {
            if (FailedServers.Contains(tfsName))
                return null;
            if (Collections.TryGetValue(tfsName, out var col))
                return col;
            try {
                col = CreateTfsCollection(tfsName);
            }
            catch (Exception) {
                FailedServers.Add(tfsName);
                return null;
            }
            Collections[tfsName] = col;
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
            //Slow (network round trip, may prompt the user), so callers should go through GetOrCreateTfsCollection which caches the result.
            //Works for VS 2017+, with assemblies from VS 15. Does not work when compiling the VSIX from VS 2015 (will cause an exception saying the assembly could not be found).
            Uri fullyQualifiedUriForName = TfsTeamProjectCollection.GetFullyQualifiedUriForName(tfsName);
            if (Resolvers.IdeHelper.VsMajorVersion < 17) { 
                var vssCredentials = VssClientCredentials.LoadCachedCredentials(fullyQualifiedUriForName, false, Microsoft.VisualStudio.Services.Common.CredentialPromptType.DoNotPrompt);
                var tfsTeamProjectCollection = new TfsTeamProjectCollection(fullyQualifiedUriForName, vssCredentials);
                return tfsTeamProjectCollection;
            }
            //Only works with VS2022+
            var vssCredentials2 = new VssClientCredentials(useDefaultCredentials: true);
            var tfsTeamProjectCollection2 = new TfsTeamProjectCollection(fullyQualifiedUriForName, vssCredentials2);
            tfsTeamProjectCollection2.Authenticate(); //May prompt the user. Throws if authentication fails or is cancelled.
            return tfsTeamProjectCollection2;
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
