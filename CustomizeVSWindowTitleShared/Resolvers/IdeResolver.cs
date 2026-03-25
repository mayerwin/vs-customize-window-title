using System;
using System.Globalization;
using System.Threading;

namespace ErwinMayerLabs.RenameVSWindowTitle.Resolvers {
    public static class IdeHelper {
        private static int? _VsMajorVersion;
        public static int VsMajorVersion {
            get {
                if (!_VsMajorVersion.HasValue) {
                    Version v;
                    _VsMajorVersion = Version.TryParse(Globals.DTE.Version, out v) ? v.Major : 10;
                }
                return _VsMajorVersion.Value;
            }
        }

        private static int? _VsMajorVersionYear;
        public static int VsMajorVersionYear => _VsMajorVersionYear ?? (_VsMajorVersionYear = GetYearFromVsMajorVersion(VsMajorVersion)).Value;

        private static int GetYearFromVsMajorVersion(int version) {
            switch (version) {
                case 9:
                    return 2008;
                case 10:
                    return 2010;
                case 11:
                    return 2012;
                case 12:
                    return 2013;
                case 14:
                    return 2015;
                case 15:
                    return 2017;
                case 16:
                    return 2019;
                case 17:
                    return 2022;
                case 18:
                    return 2026;
                default:
                    return version + 2008;
            }
        }
    }

    public class IdeNameResolver : SimpleTagResolver {
        public IdeNameResolver() : base(tagName: "ideName") { }

        public override string Resolve(AvailableInfo info) {
            return info.IdeName ?? string.Empty;
        }
    }

    public class VsProcessIdResolver : SimpleTagResolver {
        public VsProcessIdResolver() : base(tagName: "vsProcessID") { }

        public override string Resolve(AvailableInfo info) {
            return VsProcessId.Value.ToString(CultureInfo.InvariantCulture);
        }

        public static readonly Lazy<int> VsProcessId = new Lazy<int>(() => {
            using (var process = System.Diagnostics.Process.GetCurrentProcess()) {
                return process.Id;
            }
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public class VsMajorVersionResolver : SimpleTagResolver {
        public VsMajorVersionResolver() : base(tagName: "vsMajorVersion") { }

        public override string Resolve(AvailableInfo info) {
            return IdeHelper.VsMajorVersion.ToString(CultureInfo.InvariantCulture);
        }
    }

    public class VsMajorVersionYearResolver : SimpleTagResolver {
        public VsMajorVersionYearResolver() : base(tagName: "vsMajorVersionYear") { }

        public override string Resolve(AvailableInfo info) {
            return IdeHelper.VsMajorVersionYear.ToString(CultureInfo.InvariantCulture);
        }
    }
}