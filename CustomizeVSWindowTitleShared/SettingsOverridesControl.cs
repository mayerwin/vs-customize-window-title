using ErwinMayerLabs.RenameVSWindowTitle.Resolvers;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace ErwinMayerLabs.RenameVSWindowTitle {
    public partial class SettingsOverridesControl {
        public SettingsOverridesControl(SettingsOverridesPageGrid optionsPage) {
            this.InitializeComponent();
            this.OptionsPage = optionsPage;
            this.propertyGrid1.SelectedObject = this.OptionsPage;
        }

        private void SettingsOverridesControl_Paint(object sender, PaintEventArgs e) {
            this.ResizeDescriptionArea(this.propertyGrid1, lines: 7);
        }

        private readonly SettingsOverridesPageGrid OptionsPage;

        private void ResizeDescriptionArea(PropertyGrid grid, int lines) {
            try {
                var info = grid.GetType().GetProperty("Controls");
                var collection = (ControlCollection)info.GetValue(grid, null);

                foreach (var control in collection) {
                    var type = control.GetType();

                    if ("DocComment" == type.Name) {
                        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
                        var field = type.BaseType.GetField("userSized", Flags);
                        field.SetValue(control, true);

                        info = type.GetProperty("Lines");
                        info.SetValue(control, lines, null);

                        grid.HelpVisible = true;
                        break;
                    }
                }
            }

            catch (Exception ex) {
                //Trace.WriteLine(ex);
            }
        }

        string _SolutionFp;

        public string SolutionFp {
            get {
                return this._SolutionFp;
            }
            set {
                this._SolutionFp = value;
                this.btSolutionConfig.Enabled = !string.IsNullOrEmpty(value);
                this.btSolutionConfig.Tag = this.btSolutionConfig.Enabled ? value + Globals.SolutionSettingsOverrideExtension : null;
            }
        }

        string _GlobalSettingsFp;

        public string GlobalSettingsFp {
            get {
                return this._GlobalSettingsFp;
            }
            set {
                this._GlobalSettingsFp = value;

                this.btGlobalConfig.Tag = value;
                this.btGlobalConfig.Enabled = !string.IsNullOrEmpty(value);
            }
        }


        private void btGlobalConfig_Click(object sender, EventArgs e) {
            CustomizeVSWindowTitle.CurrentPackage.UiSettingsOverridesOptions.GlobalSolutionSettingsOverridesFp = this._GlobalSettingsFp;

            if (this._GlobalSettingsFp != null) {
                try {
                    this.OpenText(this._GlobalSettingsFp, true);
                }
                catch {
                    MessageBox.Show("A problem occurred while trying to open/create the file. Please check that the path is well formed and retry.", "Rename Visual Studio Window Title",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            else {
                MessageBox.Show("Please enter a file path and retry (if the file does not exist, a sample one will be created)", "Rename Visual Studio Window Title",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btSolutionConfig_Click(object sender, EventArgs e) {
            if (this.btSolutionConfig.Tag != null) {
                if (Globals.DTE.Solution == null) {
                    MessageBox.Show("Please open a solution first.", "Rename Visual Studio Window Title",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try {
                    this.OpenText(this.btSolutionConfig.Tag.ToString(), false);
                }
                catch {
                    MessageBox.Show("A problem occurred while trying to open/create the file. Please check that the path is well formed and retry.", "Rename Visual Studio Window Title",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        /// <summary>
        /// Opens a settings configuration file for editing, creating it with default template if it doesn't exist.
        /// </summary>
        /// <param name="path">The file path to the settings configuration file to open.</param>
        /// <param name="bGlobal">If true, indicates this is a global settings file and includes sample path attributes; if false, indicates a solution-specific settings file.</param>
        /// <remarks>
        /// When the file doesn't exist, this method creates a new XML document with:
        /// - A root "CustomizeVSWindowTitle" element containing configuration settings
        /// - A SettingsSet element with current solution settings (if available)
        /// - A SettingsSet-Example element showing sample configuration format
        /// 
        /// After creation, the file is opened in the IDE for editing. If newly created, the file is deleted
        /// after opening to preserve the "unsaved" state in the editor.
        /// </remarks>
        private void OpenText(string path, bool bGlobal) {
            var settings = CustomizeVSWindowTitle.CurrentPackage?.GetSettings(this._SolutionFp);
            if (settings == null) return;
            var sampleSln = Globals.GetExampleSolution(this._SolutionFp);
            var bIsNewFile = !File.Exists(path);
            if (bIsNewFile) {
                var doc = new XmlDocument();
                doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", string.Empty));
                var rootNode = doc.CreateElement("CustomizeVSWindowTitle");
                doc.AppendChild(rootNode);
                rootNode.AppendChild(doc.CreateComment(" The following SettingsSet was created based on the current configuration. All overrides are specified as attributes. Attributes equal to the default values have their name preceded by __ for informational purposes, and will be ignored unless the __ prefix is removed. "));
                XmlElement s;
                if (!string.IsNullOrEmpty(this._SolutionFp)) {
                    var sn = SolutionNameResolver.GetSolutionNameOrEmpty(settings.SolutionFilePath);

                    s = doc.CreateElement("SettingsSet");
                    if (bGlobal) {
                        addAttr(doc, s, Globals.PathTag, sampleSln, false);
                    }
                    addAttr(doc, s, Globals.SolutionNameTag, settings.SolutionName, sn.Equals(settings.SolutionName));
                    addAttr(doc, s, Globals.ClosestParentDepthTag, settings.ClosestParentDepth?.ToString(), settings.ClosestParentDepth == CustomizeVSWindowTitle.DefaultClosestParentDepth);
                    addAttr(doc, s, Globals.FarthestParentDepthTag, settings.FarthestParentDepth?.ToString(), settings.FarthestParentDepth == CustomizeVSWindowTitle.DefaultFarthestParentDepth);
                    addAttr(doc, s, Globals.AppendedStringTag, settings.AppendedString, settings.AppendedString == CustomizeVSWindowTitle.DefaultAppendedString);
                    addAttr(doc, s, Globals.PatternIfRunningModeTag, settings.PatternIfRunningMode, settings.PatternIfRunningMode == CustomizeVSWindowTitle.DefaultPatternIfRunningMode);
                    addAttr(doc, s, Globals.PatternIfBreakModeTag, settings.PatternIfBreakMode, settings.PatternIfBreakMode == CustomizeVSWindowTitle.DefaultPatternIfBreakMode);
                    addAttr(doc, s, Globals.PatternIfDesignModeTag, settings.PatternIfDesignMode, settings.PatternIfDesignMode == CustomizeVSWindowTitle.DefaultPatternIfDesignMode);
                    addAttr(doc, s, Globals.GitWorkingDirectoryTag, settings.GitWorkingDirectory, string.IsNullOrEmpty(settings.GitWorkingDirectory));
                    rootNode.AppendChild(s);
                }

                rootNode.AppendChild(doc.CreateComment(" The following is an example SettingsSet (remove -Example from the element name to use). All overrides are specified as attributes." +
                    (bGlobal ? " There can be as many SettingsSet elements as needed, with the addition of a Path attribute or child node(s) to specify to which solution path(s) or solution name(s) each SettingsSet is applicable (case-insensitive wildcards are supported). Overrides from a global settings file take precedence over those found in .sln.rn.xml files." : "") + " "));
                s = doc.CreateElement("SettingsSet-Example");
                if (bGlobal) {
                    addAttr(doc, s, Globals.PathTag, sampleSln, false);
                }
                addAttr(doc, s, Globals.SolutionNameTag, settings.SolutionName, false);
                addAttr(doc, s, Globals.ClosestParentDepthTag, CustomizeVSWindowTitle.DefaultClosestParentDepth.ToString(), false);
                addAttr(doc, s, Globals.FarthestParentDepthTag, CustomizeVSWindowTitle.DefaultFarthestParentDepth.ToString(), false);
                addAttr(doc, s, Globals.AppendedStringTag, CustomizeVSWindowTitle.DefaultAppendedString, false);
                addAttr(doc, s, Globals.PatternIfRunningModeTag, CustomizeVSWindowTitle.DefaultPatternIfRunningMode, false);
                addAttr(doc, s, Globals.PatternIfBreakModeTag, CustomizeVSWindowTitle.DefaultPatternIfBreakMode, false);
                addAttr(doc, s, Globals.PatternIfDesignModeTag, CustomizeVSWindowTitle.DefaultPatternIfDesignMode, false);
                addAttr(doc, s, Globals.GitWorkingDirectoryTag, "", false);
                rootNode.AppendChild(s);

                var tmp = Path.GetTempFileName();
                var xws = new XmlWriterSettings {
                    NewLineOnAttributes = true,
                    CloseOutput = true,
                    ConformanceLevel = ConformanceLevel.Document,
                    Encoding = System.Text.Encoding.UTF8,
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = Environment.NewLine,
                    NewLineHandling = NewLineHandling.Entitize
                };
                var xw = XmlWriter.Create(tmp, xws);
                doc.Save(xw);
                xw.Close();
                File.Move(tmp, path);
            }
            var dd = Globals.DTE.Application.Documents.Open(path);
            if (bIsNewFile) {
                dd.Saved = false;
                File.Delete(path);
            }
        }

        private static void addVal(XmlDocument doc, XmlElement S, string n, string val, bool asComment = false) {
            if (val == null)
                return;
            var E = doc.CreateElement(n);
            E.InnerText = val ?? string.Empty;

            if (asComment) {
                return;
                S.AppendChild(doc.CreateComment(E.OuterXml));
            }
            else {
                S.AppendChild(E);
            }

        }
        private static void addAttr(XmlDocument doc, XmlElement S, string n, string val, bool asComment = false) {
            if (val == null)
                return;
            S.SetAttribute(asComment ? "__" + n : n, val);
        }
    }
}