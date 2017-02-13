using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

//http://stackoverflow.com/questions/24291249/dialogpage-string-array-not-persisted
//http://www.codeproject.com/Articles/351172/CodeStash-a-journey-into-the-dark-side-of-Visual-S

namespace ErwinMayerLabs.RenameVSWindowTitle {
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    public class GlobalSettingsPageGrid : DialogPage {
        [Category("General")]
        [DisplayName("Always rewrite titles")]
        [Description("Default: true. If true, will rewrite titles even if no conflict with other instances is detected. Conflicts may not be detected if patterns have been changed from the default.")]
        [DefaultValue(true)]
        public bool AlwaysRewriteTitles { get; set; } = true;

        [Category("General")]
        [DisplayName("Farthest parent folder depth")]
        [Description("Default: 1. Distance of the farthest parent folder to be shown. 1 will only show the folder of the opened projet/solution file, before the project/folder name")]
        [DefaultValue(RenameVSWindowTitle.DefaultFarthestParentDepth)]
        public int FarthestParentDepth { get; set; } = 1;

        [Category("General")]
        [DisplayName("Closest parent folder depth")]
        [Description("Default: 1. Distance of the closest parent folder to be shown. 1 will only show the folder of the opened projet/solution file, before the project/folder name.")]
        [DefaultValue(RenameVSWindowTitle.DefaultClosestParentDepth)]
        public int ClosestParentDepth { get; set; } = 1;

        [Category("Patterns")]
        [DisplayName("No document or solution open")]
        [Description("Default: [ideName]. See 'Supported Tags' section on the left for more guidance.")]
        [DefaultValue(RenameVSWindowTitle.DefaultPatternIfNothingOpen)]
        [EditorAttribute(typeof(EditablePatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string PatternIfNothingOpen { get; set; } = RenameVSWindowTitle.DefaultPatternIfNothingOpen;

        [Category("Patterns")]
        [DisplayName("Document (no solution) open")]
        [Description("Default: [documentName] - [ideName]. See 'Supported tags' section on the left for more guidance.")]
        [DefaultValue(RenameVSWindowTitle.DefaultPatternIfDocumentButNoSolutionOpen)]
        [Editor(typeof(EditablePatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [PreviewRequires(PreviewRequiresAttribute.Requirement.Document)]
        public string PatternIfDocumentButNoSolutionOpen { get; set; } = RenameVSWindowTitle.DefaultPatternIfDocumentButNoSolutionOpen;

        [Category("Patterns")]
        [DisplayName("Solution in design mode")]
        [Description("Default: [parentPath]\\[solutionName] - [ideName]. See 'Supported tags' section on the left for more guidance.")]
        [DefaultValue("[parentPath]\\[solutionName] - [ideName]")]
        [Editor(typeof(EditablePatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [PreviewRequires(PreviewRequiresAttribute.Requirement.Solution)]
        public string PatternIfDesignMode { get; set; } = "[parentPath]\\[solutionName] - [ideName]";

        [Category("Patterns")]
        [DisplayName("Solution in break mode")]
        [Description("Default: [parentPath]\\[solutionName] (Debugging) - [ideName]. The appended string parameter will be added at the end automatically to identify that the title is being improved. See 'Supported tags' section on the left for more guidance.")]
        [DefaultValue("[parentPath]\\[solutionName] (Debugging) - [ideName]")]
        [Editor(typeof(EditablePatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [PreviewRequires(PreviewRequiresAttribute.Requirement.Solution)]
        public string PatternIfBreakMode { get; set; } = "[parentPath]\\[solutionName] (Debugging) - [ideName]";

        [Category("Patterns")]
        [DisplayName("Solution in running mode")]
        [Description("Default: [parentPath]\\[solutionName] (Running) - [ideName]. The appended string parameter will be added at the end automatically to identify that the title is being improved. See 'Supported tags' section on the left for more guidance.")]
        [DefaultValue("[parentPath]\\[solutionName] (Running) - [ideName]")]
        [Editor(typeof(EditablePatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [PreviewRequires(PreviewRequiresAttribute.Requirement.Solution)]
        public string PatternIfRunningMode { get; set; } = "[parentPath]\\[solutionName] (Running) - [ideName]";

        [Category("Patterns")]
        [DisplayName("Appended string")]
        [Description("Default: '*'. String to be added at the end of the title to identify that it has been rewritten. If not default and Always rewrite titles, the detection of concurrent instances with the same title may not work.")]
        [DefaultValue(RenameVSWindowTitle.DefaultAppendedString)]
        public string AppendedString { get; set; } = RenameVSWindowTitle.DefaultAppendedString;

        [Category("Patterns")]
        [DisplayName("Elevated string")]
        [Description("Default: '(elevated)'. String to render when [elevated] renders.")]
        [DefaultValue(RenameVSWindowTitle.DefaultAppendedString)]
        public string ElevatedString { get; set; } = RenameVSWindowTitle.DefaultElevatedString;

        [Category("Source control")]
        [DisplayName("Git binaries directory")]
        [Description("Default: Empty. Search windows PATH for git if empty.")]
        [Editor(typeof(FilePickerEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [FilePicker(true, Globals.GitExecFn, "Git executable(git.exe)|git.exe|All files(*.*)|*.*", 1)]
        [DefaultValue("")]
        public string GitDirectory { get; set; } = "";

        [Category("Source control")]
        [DisplayName("Hg binaries directory")]
        [Description("Default: Empty. Search windows PATH for hg if empty.")]
        [Editor(typeof(FilePickerEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [FilePicker(true, Globals.GitExecFn, "Git executable(hg.exe)|hg.exe|All files(*.*)|*.*", 1)]
        [DefaultValue("")]
        public string HgDirectory { get; set; } = "";

        [Category("Debug")]
        [DisplayName("Enable debug mode")]
        [Description("Default: false. Set to true to activate debug output to Output window.")]
        [DefaultValue(false)]
        public bool EnableDebugMode { get; set; } = false;

        public event EventHandler SettingsChanged;

        protected override void OnApply(PageApplyEventArgs e) {
            base.OnApply(e);
            if (e.ApplyBehavior != ApplyKind.Apply)
                return;
            this.SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}