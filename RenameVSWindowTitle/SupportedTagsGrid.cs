using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace ErwinMayerLabs.RenameVSWindowTitle {
    public class SupportedTagsGrid : DialogPage {
        private readonly Dictionary<string, string> SupportedTags  = new Dictionary<string, string> {
            { "[documentName]", "Active document or window name." },
            { "[projectName]", "Active project name." },
            { "[solutionName]", "Active solution name." },
            { "[parentX]", "Parent folder at the specified depth X (e.g. 1 for document/solution file parent folder)." },
            { "[parentPath]", "Current solution path or, if no solution open, document path, with depth range as set in settings." },
            { "[ideName]", "Name of the IDE (e.g. Microsoft Visual Studio)." },
            { "[configurationName]", "Current configuration name (e.g. Release)." },
            { "[platformName]", "Current platform name (e.g. x86)." },
            { "[vsMajorVersion]", "Major version of Visual Studio (e.g. 9, 10, 11, 12, 14...)." },
            { "[vsMajorVersionYear]", "Major version of Visual Studio, in year form (e.g. 2008, 2010, 2012, 2013, 2015...)." },
            { "[gitBranchName]", "Current Git branch name. Make sure Git\'s executable directory is added to the Windows PATH variable." },
            { "[workspaceName]", "Current Team Foundation Server (TFS) workspace name." },
            { "[workspaceOwnerName]", "Current Team Foundation Server (TFS) workspace owner name." }
        };

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected override IWin32Window Window {
            get {
                var supportedTagsControl = new SupportedTagsControl {
                    Dock = DockStyle.Fill,
                    Location = new System.Drawing.Point(0, 0)
                };
                var wb = supportedTagsControl.WebBrowser;
                var html = "<html><head><meta http-equiv=\'Content-Type\' content=\'text/html;charset=UTF-8\' /><style type=\'text/css\'>" + Resources.style + "</style><script type=\'text/javascript\'>" + Resources.script + "</script></head>";
                html += "<body oncontextmenu=\'return false;\'><h1>Supported tags (clicking on a tag will copy it to the clipboard)</h1><table><thead><tr><th>Tag</th><th>Description</th></tr></thead><tbody>";
                foreach (var tag in this.SupportedTags) {
                    html += "<tr><td onclick=\'selectText(this)\'><strong>" + tag.Key + "</strong></td>";
                    html += "<td>" + tag.Value + "</td></tr>";
                }
                html += "</tbody></table></body></html>";
                wb.DocumentText = html;
                return supportedTagsControl;
            }
        }
    }
}