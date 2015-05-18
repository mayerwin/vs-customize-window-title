namespace ErwinMayerLabs.RenameVSWindowTitle
{
    public partial class SupportedTagsControl : System.Windows.Forms.UserControl
	{
		
		//UserControl overrides dispose to clean up the component list.
		[System.Diagnostics.DebuggerNonUserCode()]protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing && components != null)
				{
					components.Dispose();
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		//Required by the Windows Form Designer
		private System.ComponentModel.Container components = null;
		
		//NOTE: The following procedure is required by the Windows Form Designer
		//It can be modified using the Windows Form Designer.
		//Do not modify it using the code editor.
		[System.Diagnostics.DebuggerStepThrough()]private void InitializeComponent()
		{
			this.WebBrowser = new System.Windows.Forms.WebBrowser();
			this.SuspendLayout();
			//
			//WebBrowser
			//
			this.WebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
			this.WebBrowser.IsWebBrowserContextMenuEnabled = false;
			this.WebBrowser.Location = new System.Drawing.Point(0, 0);
			this.WebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
			this.WebBrowser.Name = "WebBrowser";
			this.WebBrowser.Size = new System.Drawing.Size(150, 150);
			this.WebBrowser.TabIndex = 0;
			this.WebBrowser.WebBrowserShortcutsEnabled = false;
			//
			//SupportedTagsControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF((float) (6.0F), (float) (13.0F));
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.WebBrowser);
			this.Name = "SupportedTagsControl";
			this.ResumeLayout(false);
			
		}
		internal System.Windows.Forms.WebBrowser WebBrowser;
		
	}
	
}
