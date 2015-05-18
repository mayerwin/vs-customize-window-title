using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

//http://stackoverflow.com/questions/24291249/dialogpage-string-array-not-persisted
//http://www.codeproject.com/Articles/351172/CodeStash-a-journey-into-the-dark-side-of-Visual-S

namespace ErwinMayerLabs.RenameVSWindowTitle {
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    public class OptionPageGrid : DialogPage {
        private bool _AlwaysRewriteTitles = true;

        [Category("General")]
        [DisplayName("Always rewrite titles")]
        [Description("If set to true, will rewrite titles even if no conflict with other instances is detected. Default: true. Conflicts may not be detected if patterns have been changed from the default.")]
        public bool AlwaysRewriteTitles {
            get { return this._AlwaysRewriteTitles; }
            set { this._AlwaysRewriteTitles = value; }
        }
        
        private int _FarthestParentDepth = 1;

        [Category("General")]
        [DisplayName("Farthest parent folder depth")]
        [Description("Distance of the farthest parent folder to be shown. 1 will only show the folder of the opened projet/solution file, before the project/folder name. Default: 1.")]
        public int FarthestParentDepth {
            get { return this._FarthestParentDepth; }
            set { this._FarthestParentDepth = value; }
        }

        private int _ClosestParentDepth = 1;

        [Category("General")]
        [DisplayName("Closest parent folder depth")]
        [Description("Distance of the closest parent folder to be shown. 1 will only show the folder of the opened projet/solution file, before the project/folder name. Default: 1.")]
        public int ClosestParentDepth {
            get { return this._ClosestParentDepth; }
            set { this._ClosestParentDepth = value; }
        }

        private string _PatternIfNothingOpen = "[ideName]";

        [Category("Patterns")]
        [DisplayName("No document or solution open")]
        [Description("Default: [ideName]. See 'Supported Tags' section on the left or the documentation on the Visual Studio Gallery for more guidance.")]
        public string PatternIfNothingOpen {
            get { return this._PatternIfNothingOpen; }
            set { this._PatternIfNothingOpen = value; }
        }

        private string _PatternIfDocumentButNoSolutionOpen = "[documentName] - [ideName]";

        [Category("Patterns")]
        [DisplayName("Document (no solution) open")]
        [Description("Default: [documentName] - [ideName]. See 'Supported tags' section on the left or the documentation on the Visual Studio Gallery for more guidance.")]
        public string PatternIfDocumentButNoSolutionOpen {
            get { return this._PatternIfDocumentButNoSolutionOpen; }
            set { this._PatternIfDocumentButNoSolutionOpen = value; }
        }

        private string _PatternIfDesignMode = "[parentPath]\\[solutionName] - [ideName]";

        [Category("Patterns")]
        [DisplayName("Solution in design mode")]
        [Description("Default: [parentPath]\\[solutionName] - [ideName]. See 'Supported tags' section on the left or the documentation on the Visual Studio Gallery for more guidance.")]
        public string PatternIfDesignMode {
            get { return this._PatternIfDesignMode; }
            set { this._PatternIfDesignMode = value; }
        }

        private string _PatternIfBreakMode = "[parentPath]\\[solutionName] (Debugging) - [ideName]";

        [Category("Patterns")]
        [DisplayName("Solution in break mode")]
        [Description("Default: [parentPath]\\[solutionName] (Debugging) - [ideName]. The appended string parameter will be added at the end automatically to identify that the title is being improved. See 'Supported tags' section on the left or the documentation on the Visual Studio Gallery for more guidance.")]
        public string PatternIfBreakMode {
            get { return this._PatternIfBreakMode; }
            set { this._PatternIfBreakMode = value; }
        }

        private string _PatternIfRunningMode = "[parentPath]\\[solutionName] (Running) - [ideName]";

        [Category("Patterns")]
        [DisplayName("Solution in running mode")]
        [Description("Default: [parentPath]\\[solutionName] (Running) - [ideName]. The appended string parameter will be added at the end automatically to identify that the title is being improved. See 'Supported tags' section on the left or the documentation on the Visual Studio Gallery for more guidance.")]
        public string PatternIfRunningMode {
            get { return this._PatternIfRunningMode; }
            set { this._PatternIfRunningMode = value; }
        }

        private string _AppendedString = "*";

        [Category("Patterns")]
        [DisplayName("Appended string")]
        [Description("String to be added at the end of the title to identify that it has been rewritten. If not default and Always rewrite titles, the detection of concurrent instances with the same title may not work. Default: '*'.")]
        public string AppendedString {
            get { return this._AppendedString; }
            set { this._AppendedString = value; }
        }
        
        [Category("Debug")]
        [DisplayName("Enable debug mode")]
        [Description("Set to true to activate debug output to Output window.")]
        public bool EnableDebugMode { get; set; }
    }
}