using System;
using System.Reflection;

namespace ErwinMayerLabs.RenameVSWindowTitle {
    //Credit: https://github.com/pasztorpisti/vs-window-title-changer/
    class WorkspaceInfoGetter {
        static WorkspaceInfoGetter _Instance;

        public static WorkspaceInfoGetter Instance() {
            return _Instance ?? (_Instance = new WorkspaceInfoGetter());
        }

        public string GetOwner(string path) {
            return this.GetStringWorkspaceProperty(path, this._PIOwnerName);
        }

        public string GetName(string path) {
            return this.GetStringWorkspaceProperty(path, this._PIName);
        }

        string GetStringWorkspaceProperty(string path, PropertyInfo pi) {
            if (path.Length == 0 || pi == null) 
                return "";
            var workstation = this._PICurrent.GetValue(null, null);
            if (workstation == null)
                return "";
            var workspace_info = this._MIGetLocalWorkspaceInfo.Invoke(workstation, new object[] { path });
            if (workspace_info == null)
                return "";
            var val = pi.GetValue(workspace_info, null);
            return val.ToString();
        }

        PropertyInfo _PICurrent;
        MethodInfo _MIGetLocalWorkspaceInfo;
        PropertyInfo _PIName;
        PropertyInfo _PIOwnerName;

        WorkspaceInfoGetter() {
            this.Init();
        }

        void Init() {
            var workstation_class_asm_ref = "Microsoft.TeamFoundation.VersionControl.Client.Workstation, Microsoft.TeamFoundation.VersionControl.Client, Version={0}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL";
            var workspaceinfo_class_asm_ref = "Microsoft.TeamFoundation.VersionControl.Client.WorkspaceInfo, Microsoft.TeamFoundation.VersionControl.Client, Version={0}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL";

            var workstation_class = Type.GetType(string.Format(workstation_class_asm_ref, Globals.VsMajorVersion));
            if (workstation_class != null) {
                this._PICurrent = workstation_class.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
                if (this._PICurrent != null) {
                    this._MIGetLocalWorkspaceInfo = workstation_class.GetMethod("GetLocalWorkspaceInfo", new[] { typeof(string) });
                    if (this._MIGetLocalWorkspaceInfo != null) {
                        Type workspaceinfo_class = Type.GetType(string.Format(workspaceinfo_class_asm_ref, Globals.VsMajorVersion));
                        if (workspaceinfo_class != null) {
                            this._PIName = workspaceinfo_class.GetProperty("Name");
                            this._PIOwnerName = workspaceinfo_class.GetProperty("OwnerName");
                        }
                    }
                }
            }
        }
    }
}