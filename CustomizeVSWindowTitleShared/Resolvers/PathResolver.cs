using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ErwinMayerLabs.RenameVSWindowTitle.Resolvers {
    public class PathResolver : TagResolver, ISimpleTagResolver {
        private const string tagName = "path";

        public PathResolver() : base(tagNames: new[] { tagName, tagName + ":X", tagName + ":X:Y" }) { }

        public string TagName { get; } = tagName;

        public string Resolve(AvailableInfo info) {
            return string.IsNullOrEmpty(info.Path) ? info.WindowName : info.Path;
        }

        public override bool TryResolve(string tag, AvailableInfo info, out string s) {
            s = null;
            if (!tag.StartsWith(tagName, StringComparison.InvariantCulture)) return false;
            var m = Globals.RangeRegex.Match(tag.Substring(tagName.Length));
            if (m.Success) {
                if (!info.PathParts.Any()) {
                    s = string.Empty;
                }
                else {
                    var x = int.Parse(m.Groups["startIndex"].Value, CultureInfo.InvariantCulture);
                    var y = int.Parse(m.Groups["endIndex"].Value, CultureInfo.InvariantCulture);
                    var pathRange = Globals.GetPathRange(info.PathParts, x, y);
                    s = Globals.GetPathForTitle(pathRange);
                }
                return true;
            }
            m = Globals.IndexRegex.Match(tag.Substring(tagName.Length));
            if (m.Success) {
                if (!info.PathParts.Any()) {
                    s = string.Empty;
                }
                else {
                    var index = int.Parse(m.Groups["index"].Value, CultureInfo.InvariantCulture);
                    s = Globals.GetPathPart(info.PathParts, index);
                }
                return true;
            }
            return false;
        }
    }

    public class ParentResolver : TagResolver {
        private const string tagName = "parent";

        public ParentResolver() : base(tagNames: new[] { tagName + ":X", tagName + ":X:Y" }) { }

        public override bool TryResolve(string tag, AvailableInfo info, out string s) {
            s = null;
            if (!tag.StartsWith(tagName, StringComparison.InvariantCulture)) return false;

            var m = Globals.RangeRegex.Match(tag.Substring(tagName.Length));
            if (m.Success) {
                if (!info.PathParts.Any()) {
                    s = string.Empty;
                }
                else {
                    var x = int.Parse(m.Groups["startIndex"].Value, CultureInfo.InvariantCulture);
                    var y = int.Parse(m.Groups["endIndex"].Value, CultureInfo.InvariantCulture);
                    var pathRange = Globals.GetPathRange(info.PathParts, -(x + 1), -(y + 1));
                    s = Globals.GetPathForTitle(pathRange);
                }
                return true;
            }
            m = Globals.IndexRegex.Match(tag.Substring(tagName.Length));
            if (m.Success) {
                if (!info.PathParts.Any()) {
                    s = string.Empty;
                }
                else {
                    var index = int.Parse(m.Groups["index"].Value, CultureInfo.InvariantCulture);
                    s = Globals.GetPathPart(info.PathParts, -(index + 1));
                }
                return true;
            }
            return false;
        }
    }

    public class ParentPathResolver : SimpleTagResolver {
        public ParentPathResolver() : base(tagName: "parentPath") { }

        public override string Resolve(AvailableInfo info) {
            return AvailableInfo.GetParentPath(info.PathParts, info.Cfg?.ClosestParentDepth ?? info.GlobalSettings.ClosestParentDepth, info.Cfg?.FarthestParentDepth ?? info.GlobalSettings.FarthestParentDepth) ?? string.Empty;
        }
    }

    public class EnvResolver : TagResolver {
        static readonly Regex EnvRegex = new Regex(@"^env:(.+)$", RegexOptions.Compiled);

        public EnvResolver() : base(tagNames: new[] { "env:X" }) { }

        public override bool TryResolve(string tag, AvailableInfo info, out string s) {
            s = null;
            var m = EnvRegex.Match(tag);
            if (m.Success) {
                var name = m.Groups[1].Value;
                if (name.ToLowerInvariant() == "machinename") name = "COMPUTERNAME";
                s = Environment.GetEnvironmentVariable(name);
                return true;
            }
            return false;
        }
    }
}