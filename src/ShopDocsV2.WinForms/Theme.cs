namespace ShopDocsV2.WinForms;

/// <summary>
/// The main window's earth-tone palette, taken from the logo (wood brown, copper, parchment, charcoal),
/// plus helpers that apply it. Kept light on purpose: parchment backgrounds, dark menu/status bars like the
/// banner, and copper only as an accent. Text inputs stay white so they read as the places to type.
/// </summary>
internal static class Theme
{
    public static readonly Color Charcoal = Color.FromArgb(0x2B, 0x26, 0x22);
    public static readonly Color Wood = Color.FromArgb(0x7A, 0x4A, 0x26);
    public static readonly Color Copper = Color.FromArgb(0x9C, 0x5F, 0x2C);
    public static readonly Color CopperHover = Color.FromArgb(0xB0, 0x72, 0x3A);
    public static readonly Color CopperPressed = Color.FromArgb(0x85, 0x4F, 0x23);
    public static readonly Color Parchment = Color.FromArgb(0xF6, 0xF0, 0xE6);
    public static readonly Color Sand = Color.FromArgb(0xEA, 0xDC, 0xC6);
    public static readonly Color Highlight = Color.FromArgb(0xEF, 0xD8, 0xB8);
    public static readonly Color GridLine = Color.FromArgb(0xDC, 0xCA, 0xB0);
    public static readonly Color AlternateRow = Color.FromArgb(0xFB, 0xF7, 0xF1);
    public static readonly Color Cream = Color.FromArgb(0xF5, 0xEB, 0xDD);

    public static readonly Font HeadingFont = new("Segoe UI", 9F, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 9F);

    /// <summary>Renderer for the menu and status bars: charcoal bars with cream text, cream dropdowns with a copper-tan highlight.</summary>
    public static ToolStripRenderer BarRenderer { get; } = new ThemeBarRenderer();

    /// <summary>Bold wood-brown caption; children are reset to the regular font and text color so only the caption changes.</summary>
    public static void StyleGroupBox(GroupBox groupBox, Control content)
    {
        groupBox.Font = HeadingFont;
        groupBox.ForeColor = Wood;
        content.Font = BodyFont;
        content.ForeColor = Charcoal;
    }

    /// <summary>The solid copper look for a group's primary action (the list add buttons).</summary>
    public static void StyleAccentButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = Copper;
        button.ForeColor = Color.White;
        button.FlatAppearance.BorderColor = CopperPressed;
        button.FlatAppearance.MouseOverBackColor = CopperHover;
        button.FlatAppearance.MouseDownBackColor = CopperPressed;
        button.UseVisualStyleBackColor = false;
    }

    /// <summary>A quiet flat icon button (wood-brown glyph, tan border) for small helpers like the copy buttons.</summary>
    public static void StyleIconButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = Parchment;
        button.ForeColor = Wood;
        button.FlatAppearance.BorderColor = GridLine;
        button.FlatAppearance.MouseOverBackColor = Highlight;
        button.FlatAppearance.MouseDownBackColor = Sand;
        button.UseVisualStyleBackColor = false;
    }

    /// <summary>Wood-brown column headers, faint alternating rows and a copper-tan selection.</summary>
    public static void StyleGrid(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = Color.White;
        grid.GridColor = GridLine;
        // The white grid already stands out on parchment; FixedSingle would add a hard black outline.
        grid.BorderStyle = BorderStyle.None;

        grid.ColumnHeadersDefaultCellStyle.BackColor = Wood;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Cream;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Wood;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Cream;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(2, 3, 2, 3);
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

        grid.RowHeadersDefaultCellStyle.BackColor = Sand;
        grid.RowHeadersDefaultCellStyle.SelectionBackColor = Highlight;
        grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        grid.DefaultCellStyle.SelectionBackColor = Highlight;
        grid.DefaultCellStyle.SelectionForeColor = Charcoal;
        grid.AlternatingRowsDefaultCellStyle.BackColor = AlternateRow;
    }

    private sealed class ThemeBarRenderer() : ToolStripProfessionalRenderer(new ThemeColorTable())
    {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Enabled)
            {
                e.TextColor = e.Item.IsOnDropDown ? Charcoal : Cream;
            }
            base.OnRenderItemText(e);
        }
    }

    private sealed class ThemeColorTable : ProfessionalColorTable
    {
        public ThemeColorTable() => UseSystemColors = false;

        public override Color MenuStripGradientBegin => Charcoal;
        public override Color MenuStripGradientEnd => Charcoal;
        public override Color StatusStripGradientBegin => Charcoal;
        public override Color StatusStripGradientEnd => Charcoal;

        // Top-level menu items (on the charcoal bar) when hovered or open.
        public override Color MenuItemSelectedGradientBegin => Wood;
        public override Color MenuItemSelectedGradientEnd => Wood;
        public override Color MenuItemPressedGradientBegin => Wood;
        public override Color MenuItemPressedGradientMiddle => Wood;
        public override Color MenuItemPressedGradientEnd => Wood;

        // Dropdown menus.
        public override Color MenuItemSelected => Highlight;
        public override Color MenuItemBorder => Copper;
        public override Color MenuBorder => Wood;
        public override Color ToolStripDropDownBackground => Parchment;
        public override Color ImageMarginGradientBegin => Sand;
        public override Color ImageMarginGradientMiddle => Sand;
        public override Color ImageMarginGradientEnd => Sand;
        public override Color SeparatorDark => GridLine;
        public override Color SeparatorLight => Parchment;
        public override Color CheckBackground => Highlight;
        public override Color CheckSelectedBackground => Highlight;
    }
}
