using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

/// <summary>
/// Renders a List question as an editable grid: one column per itemField, one row per RoomListItem.
/// Catalog-backed columns get a dropdown of catalog options that opens on click but still accepts
/// free-typed values (options are per row, so CatalogFilterBy works); plain-select columns get a
/// real constrained dropdown; a ColorPreview column is followed by a read-only swatch
/// column showing the finish's color with its RGB value on top (click it to copy RGB or hex).
/// </summary>
public partial class ListFieldEditor : UserControl
{
    private const string RemoveColumnName = "__remove";
    private const string SpecsColumnName = "__specs";
    private const string SwatchColumnPrefix = "__swatch_";

    private Room? _room;
    private QuestionDef? _question;
    private CatalogSnapshot? _catalog;
    private Action? _onAnswerChanged;
    private List<RoomListItem>? _items;

    /// <summary>Options for catalog-backed columns that don't depend on a CatalogFilterBy sibling, resolved once per Bind() rather than once per row.</summary>
    private readonly Dictionary<string, IReadOnlyList<string>> _unfilteredOptionsCache = new();

    public ListFieldEditor()
    {
        InitializeComponent();

        grid.DataError += (_, e) => e.ThrowException = false;
        grid.CellContentClick += Grid_CellContentClick;
        grid.CellValueChanged += Grid_CellValueChanged;
        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            // Catalog cells commit on pick (SelectionChangeCommitted) or on leaving the cell, not per
            // keystroke: a half-typed value isn't in the cell's Items yet and would fail to parse.
            if (grid.IsCurrentCellDirty && CatalogFieldAt(grid.CurrentCell?.ColumnIndex ?? -1) is null)
            {
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        grid.CellValidating += Grid_CellValidating;
        grid.EditingControlShowing += Grid_EditingControlShowing;
        grid.CellFormatting += Grid_CellFormatting;
        grid.CellClick += Grid_CellClick;
        grid.UserDeletingRow += (_, e) =>
        {
            if (e.Row?.Tag is RoomListItem item)
            {
                _items!.Remove(item);
            }
            _onAnswerChanged?.Invoke();
        };
        addButton.Click += AddButton_Click;
    }

    internal void Bind(Room room, QuestionDef question, CatalogSnapshot catalog, Action onAnswerChanged)
    {
        _room = room;
        _question = question;
        _catalog = catalog;
        _onAnswerChanged = onAnswerChanged;
        addButton.Text = string.IsNullOrWhiteSpace(question.AddLabel) ? "+ Add" : question.AddLabel;

        if (!room.ListAnswers.TryGetValue(question.Id, out var items))
        {
            items = [];
            room.ListAnswers[question.Id] = items;
        }
        _items = items;

        _unfilteredOptionsCache.Clear();
        foreach (var field in question.ItemFields ?? [])
        {
            if (string.IsNullOrEmpty(field.CatalogSource) || field.CatalogFilterBy is not null ||
                _unfilteredOptionsCache.ContainsKey(field.CatalogSource))
            {
                continue;
            }

            _unfilteredOptionsCache[field.CatalogSource] = catalog.Resolve(field.CatalogSource, null);
        }

        BuildColumns();
        LoadRows();
    }

    private void BuildColumns()
    {
        grid.Columns.Clear();

        foreach (var field in _question!.ItemFields ?? [])
        {
            DataGridViewColumn column;
            if (field.Type == QuestionType.Select && string.IsNullOrEmpty(field.CatalogSource))
            {
                var comboColumn = new DataGridViewComboBoxColumn { Name = field.Id, HeaderText = field.Label };
                comboColumn.Items.Add("");
                foreach (var option in field.Options ?? [])
                {
                    comboColumn.Items.Add(option);
                }
                column = comboColumn;
            }
            else if (!string.IsNullOrEmpty(field.CatalogSource))
            {
                // Items are filled per cell by RefreshCatalogOptions, since filtered fields differ by row.
                column = new DataGridViewComboBoxColumn { Name = field.Id, HeaderText = field.Label };
            }
            else
            {
                column = new DataGridViewTextBoxColumn { Name = field.Id, HeaderText = field.Label };
            }
            grid.Columns.Add(column);

            if (field.ColorPreview)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = SwatchColumnPrefix + field.Id,
                    HeaderText = "Swatch",
                    ReadOnly = true,
                    Width = 130,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                    Tag = field.Id
                });
            }
        }

        if (_question.Id == "appliance_package")
        {
            grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = SpecsColumnName,
                HeaderText = "",
                Text = "\U0001F50D Specs",
                UseColumnTextForLinkValue = true
            });
        }

        grid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = RemoveColumnName,
            HeaderText = "",
            Text = "✕",
            UseColumnTextForButtonValue = true,
            Width = 32,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
    }

    private void LoadRows()
    {
        grid.Rows.Clear();
        foreach (var item in _items!.OrderBy(i => i.SortOrder))
        {
            AddGridRow(item);
        }
    }

    private void AddGridRow(RoomListItem item)
    {
        var rowIndex = grid.Rows.Add();
        var row = grid.Rows[rowIndex];
        row.Tag = item;

        foreach (var field in _question!.ItemFields ?? [])
        {
            var text = item.Fields.TryGetValue(field.Id, out var value) ? value.DisplayText : "";
            if (!string.IsNullOrEmpty(field.CatalogSource))
            {
                RefreshCatalogOptions(row, field, text);
            }
            row.Cells[field.Id].Value = text;
        }
    }

    /// <summary>
    /// Refills a catalog cell's dropdown from the catalog (narrowed by its sibling when CatalogFilterBy is set).
    /// The current value stays in the list even when it isn't a catalog entry, so free-typed or
    /// since-deleted values still display instead of failing the combo cell's value check.
    /// </summary>
    private void RefreshCatalogOptions(DataGridViewRow row, QuestionDef field, string currentValue)
    {
        IReadOnlyList<string> options;
        if (field.CatalogFilterBy is null)
        {
            options = _unfilteredOptionsCache[field.CatalogSource!];
        }
        else
        {
            string? filterValue = null;
            if (row.Tag is RoomListItem item && item.Fields.TryGetValue(field.CatalogFilterBy, out var fv))
            {
                filterValue = fv.Text;
            }
            options = _catalog!.Resolve(field.CatalogSource, filterValue);
        }

        var cell = (DataGridViewComboBoxCell)row.Cells[field.Id];
        cell.Items.Clear();
        cell.Items.Add("");
        foreach (var option in options)
        {
            cell.Items.Add(option);
        }
        if (!string.IsNullOrEmpty(currentValue) && !options.Contains(currentValue))
        {
            cell.Items.Add(currentValue);
        }
    }

    private QuestionDef? CatalogFieldAt(int columnIndex)
    {
        if (columnIndex < 0)
        {
            return null;
        }

        var field = _question!.ItemFields?.FirstOrDefault(f => f.Id == grid.Columns[columnIndex].Name);
        return string.IsNullOrEmpty(field?.CatalogSource) ? null : field;
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        var newItem = new RoomListItem { Id = Guid.NewGuid(), SortOrder = _items!.Count };
        _items.Add(newItem);
        AddGridRow(newItem);
        _onAnswerChanged?.Invoke();

        var newRowIndex = grid.Rows.Count - 1;
        if (grid.Columns.Count > 0 && newRowIndex >= 0)
        {
            grid.CurrentCell = grid.Rows[newRowIndex].Cells[0];
            grid.BeginEdit(true);
        }
    }

    private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        var columnName = grid.Columns[e.ColumnIndex].Name;
        if (columnName == RemoveColumnName)
        {
            RemoveRowAt(e.RowIndex);
            return;
        }

        if (columnName == SpecsColumnName && grid.Rows[e.RowIndex].Tag is RoomListItem item)
        {
            OpenSpecsSearch(item);
        }
    }

    private void RemoveRowAt(int rowIndex)
    {
        if (grid.Rows[rowIndex].Tag is RoomListItem item)
        {
            _items!.Remove(item);
        }
        grid.Rows.RemoveAt(rowIndex);
        _onAnswerChanged?.Invoke();
    }

    private static void OpenSpecsSearch(RoomListItem item)
    {
        var model = (item.Fields.TryGetValue("model", out var modelValue) ? modelValue.Text : null)?.Trim();
        if (string.IsNullOrEmpty(model))
        {
            return;
        }

        var query = $"\"{model}\" (\"quick specs\" OR \"spec sheet\" OR \"specification sheet\")";
        var url = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Rows[e.RowIndex].Tag is not RoomListItem item)
        {
            return;
        }

        var column = grid.Columns[e.ColumnIndex];
        var itemField = _question!.ItemFields?.FirstOrDefault(f => f.Id == column.Name);
        if (itemField is null)
        {
            return;
        }

        var text = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string ?? "";
        SetFieldAnswer(item, itemField, text);

        if (itemField.ColorPreview)
        {
            grid.InvalidateCell(grid.Columns[SwatchColumnPrefix + itemField.Id]!.Index, e.RowIndex);
        }

        foreach (var dependent in _question.ItemFields!.Where(f => f.CatalogFilterBy == itemField.Id))
        {
            var row = grid.Rows[e.RowIndex];
            var depText = item.Fields.TryGetValue(dependent.Id, out var depValue) ? depValue.Text ?? "" : "";
            if (depText != "" && !_catalog!.Resolve(dependent.CatalogSource, text).Contains(depText))
            {
                item.Fields.Remove(dependent.Id);
                depText = "";
            }

            // Re-filter the dependent's dropdown to the newly chosen value.
            RefreshCatalogOptions(row, dependent, depText);
            row.Cells[dependent.Id].Value = depText;
        }

        _onAnswerChanged?.Invoke();
    }

    private static void SetFieldAnswer(RoomListItem item, QuestionDef field, string text)
    {
        if (field.Type == QuestionType.Number)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                item.Fields[field.Id] = AnswerValue.Of(number);
            }
            else
            {
                item.Fields.Remove(field.Id);
            }
        }
        else if (string.IsNullOrEmpty(text))
        {
            item.Fields.Remove(field.Id);
        }
        else
        {
            item.Fields[field.Id] = AnswerValue.Of(text);
        }
    }

    private void Grid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (e.Control is not ComboBox comboBox)
        {
            return;
        }

        // The grid reuses one editing control across cells and columns, so reset what we change here.
        comboBox.SelectionChangeCommitted -= CatalogComboBox_SelectionChangeCommitted;
        comboBox.TextUpdate -= CatalogComboBox_TextUpdate;

        if (CatalogFieldAt(grid.CurrentCell?.ColumnIndex ?? -1) is null)
        {
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.AutoCompleteMode = AutoCompleteMode.None;
            return;
        }

        // DropDown (not DropDownList) so values outside the catalog can still be typed.
        comboBox.DropDownStyle = ComboBoxStyle.DropDown;
        comboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        comboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        comboBox.SelectionChangeCommitted += CatalogComboBox_SelectionChangeCommitted;
        comboBox.TextUpdate += CatalogComboBox_TextUpdate;
    }

    // The combo editing control only reports a change on list selection; flag typed text too so a
    // free-typed value is committed (via Grid_CellValidating) when the user leaves the cell.
    private void CatalogComboBox_TextUpdate(object? sender, EventArgs e) => grid.NotifyCurrentCellDirty(true);

    private void CatalogComboBox_SelectionChangeCommitted(object? sender, EventArgs e)
    {
        // Picked from the list: the value is already in the cell's Items, so commit right away.
        // In DropDown style, Text still holds the previous value when this event fires, and the grid
        // commits Text — so sync it to the picked item first or the old value gets committed.
        if (sender is ComboBox { SelectedItem: string picked } comboBox)
        {
            comboBox.Text = picked;
        }
        grid.NotifyCurrentCellDirty(true);
        grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void Grid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (e.RowIndex < 0 || !grid.IsCurrentCellInEditMode || CatalogFieldAt(e.ColumnIndex) is null ||
            grid.EditingControl is not ComboBox comboBox)
        {
            return;
        }

        // Free-typed value: add it to this cell's Items so the combo cell accepts it on commit.
        var text = comboBox.Text.Trim();
        var cell = (DataGridViewComboBoxCell)grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (!cell.Items.Contains(text))
        {
            cell.Items.Add(text);
        }

        if (!Equals(cell.Value, text))
        {
            comboBox.Text = text;
            grid.NotifyCurrentCellDirty(true);
        }
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Tag is not string sourceFieldId)
        {
            return;
        }

        var hex = SwatchHexAt(e.RowIndex, sourceFieldId);
        e.Value = ColorMath.ToRgbLabel(hex);
        e.FormattingApplied = true;
        if (e.Value is "")
        {
            return;
        }

        var background = ColorTranslator.FromHtml(NormalizeHex(hex!));
        var foreground = ColorTranslator.FromHtml(ColorMath.GetContrastingTextColor(hex));
        e.CellStyle!.BackColor = background;
        e.CellStyle.ForeColor = foreground;
        // Keep the swatch visible (instead of the selection highlight) when the cell is selected.
        e.CellStyle.SelectionBackColor = background;
        e.CellStyle.SelectionForeColor = foreground;
    }

    private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (CatalogFieldAt(e.ColumnIndex) is not null)
        {
            // One click opens the option list (the grid otherwise only starts editing on a keystroke or F2).
            if (grid.BeginEdit(false) && grid.EditingControl is ComboBox comboBox)
            {
                comboBox.DroppedDown = true;
            }
            return;
        }

        if (grid.Columns[e.ColumnIndex].Tag is not string sourceFieldId)
        {
            return;
        }

        var hex = SwatchHexAt(e.RowIndex, sourceFieldId);
        var rgb = ColorMath.ToRgbLabel(hex);
        if (rgb == "")
        {
            return;
        }

        var normalizedHex = NormalizeHex(hex!).ToUpperInvariant();
        var menu = new ContextMenuStrip();
        menu.Items.Add($"Copy RGB ({rgb})", null, (_, _) => CopyToClipboard(rgb));
        menu.Items.Add($"Copy Hex ({normalizedHex})", null, (_, _) => CopyToClipboard(normalizedHex));
        menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
        menu.Show(Cursor.Position);
    }

    /// <summary>The catalog hex for the finish named in this row's source column, or null if none/unknown.</summary>
    private string? SwatchHexAt(int rowIndex, string sourceFieldId)
    {
        var name = grid.Rows[rowIndex].Cells[sourceFieldId].Value as string;
        return string.IsNullOrEmpty(name) ? null : _catalog!.HexFor(name);
    }

    private static string NormalizeHex(string hex) => hex.StartsWith('#') ? hex : "#" + hex;

    private void CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            MessageBox.Show(FindForm(), "The clipboard is in use by another program. Please try again.",
                "Copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
