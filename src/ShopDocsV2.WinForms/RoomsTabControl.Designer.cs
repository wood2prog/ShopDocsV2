namespace ShopDocsV2.WinForms;

partial class RoomsTabControl
{
    private System.ComponentModel.IContainer components = null;

    private TabControl tabControl = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        tabControl = new TabControl();

        SuspendLayout();

        tabControl.Dock = DockStyle.Fill;

        Controls.Add(tabControl);
        AutoScaleMode = AutoScaleMode.Dpi;
        Dock = DockStyle.Fill;

        ResumeLayout(false);
    }
}
