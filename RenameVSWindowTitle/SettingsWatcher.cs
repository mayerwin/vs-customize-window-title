using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using ErwinMayerLabs.Lib;

namespace ErwinMayerLabs.RenameVSWindowTitle {
    /// <summary>
    /// This class maintains a list of config sets loaded from a single config file.
    /// File changes are observed by a filesystem watcher and reloaded on next demand.
    /// 
    /// </summary>
    public class SettingsWatcher {
        readonly bool IsGlobalConfig;

        //loaded sets (null means reload pending, empty list means missing or error)
        List<SettingsSet> SettingsSets = new List<SettingsSet>();
        bool IsReloadingNeeded;
        string Fp;
        FileSystemWatcher Watcher;

        static readonly char[] PathSeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        public delegate void SettingsClearedDelegate();
        public SettingsClearedDelegate SettingsCleared;

        public SettingsWatcher(bool isGlobalConfig) {
            this.IsGlobalConfig = isGlobalConfig;
        }

        public void Clear() {
            this.SettingsSets.Clear();
            this.IsReloadingNeeded = true;
            this.StopWatching();
            this.SettingsCleared?.Invoke();
        }

        private void StopWatching() {
            if (this.Watcher != null) {
                // stop watching
                this.Watcher.EnableRaisingEvents = false;
                this.Watcher.Created -= this.Watcher_Changed;
                this.Watcher.Changed -= this.Watcher_Changed;
                this.Watcher.Renamed -= this.Watcher_Renamed;
                this.Watcher.Deleted -= this.Watcher_Deleted;
                this.Watcher.Dispose();
                this.Watcher = null;
            }
        }

        public void Update(string path) {
            if (this.Fp == path)
                return;
            this.Clear();
            this.Fp = path;
        }

        public bool Update(SettingsSet settings) {
            if (this.IsReloadingNeeded) {
                this.TryReloadSettings();
            }
            foreach (var settingsSet in this.SettingsSets) {
                bool bMatched;
                if (!this.IsGlobalConfig) {
                    // ignore paths
                    bMatched = true;
                }
                else {
                    bMatched = false;
                    foreach (var path in settingsSet.Paths) {
                        bMatched = (-1 == path.IndexOfAny(PathSeparators) ?
                            path.Equals(settings.SolutionFileName, StringComparison.CurrentCultureIgnoreCase) :
                            path.Equals(settings.SolutionFilePath, StringComparison.CurrentCultureIgnoreCase)) ||
                            (path.Contains("*") || path.Contains("?")) && new Wildcard(path).IsMatch(settings.SolutionFilePath);
                         if (bMatched) break;
                    }
                }
                if (bMatched) {
                    settings.Merge(settingsSet);
                    return true;
                }
            }
            return false;
        }

        private void TryReloadSettings() {
            this.IsReloadingNeeded = false;
            if (string.IsNullOrEmpty(this.Fp))
                return;

            this.StopWatching();
            try {
                this.Watcher = new FileSystemWatcher(Path.GetDirectoryName(this.Fp), Path.GetFileName(this.Fp));
            }
            catch {
                // cannot setup watcher because of missing folder
                this.Clear();
                return;
            }
            this.Watcher.Created += this.Watcher_Created;
            this.Watcher.Changed += this.Watcher_Changed;
            this.Watcher.Renamed += this.Watcher_Renamed;
            this.Watcher.Deleted += this.Watcher_Deleted;

            this.LoadSettings();

            this.Watcher.EnableRaisingEvents = true;
        }

        private static string GetAttributeOrChild(XmlElement node, string name) {
            var attr = node.GetAttributeNode(name);
            if (attr != null)
                return attr.Value;
            var child = node.GetElementsByTagName(name);
            if (child.Count > 0)
                return child[0].InnerText;
            return null;
        }

        private static void TryUpdateSetting<T>(ref T target, XmlElement node, string name) {
            try {
                var val = GetAttributeOrChild(node, name);
                if (string.IsNullOrEmpty(val))
                    return;
                target = (T)Convert.ChangeType(val, typeof(T), CultureInfo.InvariantCulture);
            }
            catch {
                // do nothing
            }
        }
        
        private void LoadSettings() {
            this.SettingsSets = new List<SettingsSet>();

            if (!File.Exists(this.Fp)) {
                Debug.WriteLine("No settings overrides {0}", this.Fp);
                return;
            }

            var doc = new XmlDocument();
            Debug.WriteLine("Read settings overrides from {0}", this.Fp);
            try {
                doc.Load(this.Fp);
            }
            catch (Exception x) {
                Debug.WriteLine("Error {0}", x);
            }

            var nodes = doc.SelectNodes("RenameVSWindowTitle/SettingsSet");

            foreach (XmlElement node in nodes) {
                var settingsSet = new SettingsSet { Paths = new List<string>() };

                // read paths (Path attribute and Path child elements)
                var path = node.GetAttribute(Globals.PathTag);
                if (!string.IsNullOrEmpty(path))
                    settingsSet.Paths.Add(path);

                var paths = node.GetElementsByTagName(Globals.PathTag);
                if (paths.Count > 0) {
                    foreach (XmlElement elem in paths) {
                        path = elem.InnerText;
                        if (!string.IsNullOrEmpty(path))
                            settingsSet.Paths.Add(path);
                    }
                }

                TryUpdateSetting(ref settingsSet.SolutionName, node, Globals.SolutionNameTag);
                TryUpdateSetting(ref settingsSet.ClosestParentDepth, node, Globals.ClosestParentDepthTag);
                TryUpdateSetting(ref settingsSet.FarthestParentDepth, node, Globals.FarthestParentDepthTag);
                TryUpdateSetting(ref settingsSet.AppendedString, node, Globals.AppendedStringTag);
                TryUpdateSetting(ref settingsSet.ElevatedString, node, Globals.ElevatedStringTag);
                TryUpdateSetting(ref settingsSet.PatternIfRunningMode, node, Globals.PatternIfRunningModeTag);
                TryUpdateSetting(ref settingsSet.PatternIfBreakMode, node, Globals.PatternIfBreakModeTag);
                TryUpdateSetting(ref settingsSet.PatternIfDesignMode, node, Globals.PatternIfDesignModeTag);

                this.SettingsSets.Add(settingsSet);
            }
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e) {
            try {
                this.Clear();
                //this.IsReloadingNeeded = false;
            }
            catch {
                // do nothing
            }
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e) {
            try {
                this.Clear();
            }
            catch {
                // do nothing
            }
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e) {
            try {
                this.Clear();
            }
            catch {
                // do nothing
            }
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e) {
            try {
                this.Clear();
            }
            catch {
                // do nothing
            }
        }
    }
}
