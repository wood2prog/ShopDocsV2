using System.Diagnostics;
using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

/// <summary>
/// Renders a List question as an editable grid: one column per itemField, one row per RoomListItem.
/// Catalog-backed columns get a dropdown of catalog options that opens on click but still accepts
/// free-typed values (options are per row, so CatalogFilterBy works); plain-select columns get a
/// real constrained dropdown; a ColorPreview column is followed by a read-only swatch
/// column showing the finish's color with its RGB value on top (click it to copy RGB or hex).
/// A CatalogLink column gets a read-only Product Page column (after the item's fields) showing the picked accessory's
/// "Name – Model#" as a link to its web page.
/// With no lines, the grid is hidden and only a placeholder and the add button show (see EmptyChanged).
/// </summary>
public partial class ListFieldEditor : UserControl
{
    private const string RemoveColumnName = "__remove";
    private const string SpecsColumnName = "__specs";
    private const string SinkSearchColumnName = "__sink_search";
    private const string SwatchColumnPrefix = "__swatch_";
    private const string ProductPageColumnPrefix = "__product_page_";

    private Room? _room;
    private QuestionDef? _question;
    private CatalogSnapshot? _catalog;
    private Action? _onAnswerChanged;
    private List<RoomListItem>? _items;
    private ComboBoxSubstringFilter? _catalogComboFilter;
    private bool? _showingEmptyState;

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
        grid.RowsAdded += (_, _) => UpdateEmptyState();
        grid.RowsRemoved += (_, _) => UpdateEmptyState();
        addButton.Click += AddButton_Click;
    }

    /// <summary>Raised when the editor switches between showing its grid and the empty placeholder, so the host can resize it.</summary>
    internal event EventHandler? EmptyChanged;

    /// <summary>True when there are no lines, so only the placeholder and add button show.</summary>
    internal bool IsEmpty => grid.Rows.Count == 0;

    /// <summary>
    /// The height that fits the placeholder and add button, for the host to use while IsEmpty. Uses the
    /// controls' current (DPI-scaled) sizes, so it's only final once the editor is on a form.
    /// </summary>
    internal int EmptyStateHeight => emptyLabel.PreferredHeight + addButtonPanel.Height + Padding.Vertical;

    private void UpdateEmptyState()
    {
        // Tracked separately: grid.Visible reads false whenever this editor's tab isn't the selected one.
        var empty = IsEmpty;
        if (_showingEmptyState == empty)
        {
            return;
        }

        _showingEmptyState = empty;
        grid.Visible = !empty;
        emptyLabel.Visible = empty;
        EmptyChanged?.Invoke(this, EventArgs.Empty);
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
        UpdateEmptyState();
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

        // Product Page links go after the item's own fields rather than beside the picker.
        foreach (var field in (_question.ItemFields ?? []).Where(f => f.CatalogLink))
        {
            grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = ProductPageColumnPrefix + field.Id,
                HeaderText = "Product Page",
                ReadOnly = true,
                TrackVisitedState = false,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
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
        else if (_question.Id == "sinks")
        {
            grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = SinkSearchColumnName,
                HeaderText = "",
                Text = "\U0001F50D Search",
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
            var text = AnswerInput.GetText(item.Fields, field.Id) ?? "";
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

    /// <summary>The item field shown in this column, or null for the extra columns (swatch, links, remove).</summary>
    private QuestionDef? FieldAt(int columnIndex) =>
        columnIndex < 0 ? null : _question!.ItemFields?.FirstOrDefault(f => f.Id == grid.Columns[columnIndex].Name);

    private QuestionDef? CatalogFieldAt(int columnIndex) =>
        FieldAt(columnIndex) is { CatalogSource: { Length: > 0 } } field ? field : null;

    /// <summary>The List question this editor shows, or null before Bind().</summary>
    internal string? QuestionId => _question?.Id;

    private void AddButton_Click(object? sender, EventArgs e) => AddItem();

    /// <summary>
    /// Puts the cursor in the first cell of a new line, ready to type. If the last line is still blank it's
    /// reused instead, so pressing the shortcut repeatedly doesn't stack up empty lines.
    /// </summary>
    internal void StartNewItem()
    {
        grid.EndEdit();
        var lastIndex = grid.Rows.Count - 1;
        if (lastIndex >= 0 && grid.Rows[lastIndex].Tag is RoomListItem last && last.Fields.Values.All(v => v.IsBlank))
        {
            grid.Focus();
            EditFirstCell(lastIndex);
            return;
        }

        AddItem();
    }

    private void AddItem()
    {
        var newItem = new RoomListItem { Id = Guid.NewGuid(), SortOrder = _items!.Count };
        _items.Add(newItem);
        AddGridRow(newItem);
        _onAnswerChanged?.Invoke();
        // Focus after the row is added: while the list was empty the grid was hidden and couldn't take focus.
        grid.Focus();
        EditFirstCell(grid.Rows.Count - 1);
    }

    private void EditFirstCell(int rowIndex)
    {
        if (grid.Columns.Count > 0 && rowIndex >= 0)
        {
            grid.CurrentCell = grid.Rows[rowIndex].Cells[0];
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

        if (ProductPageUrlAt(e.RowIndex, e.ColumnIndex) is { } url)
        {
            WebLink.Open(FindForm(), url);
            return;
        }

        if (columnName == SpecsColumnName && grid.Rows[e.RowIndex].Tag is RoomListItem item)
        {
            OpenSpecsSearch(item);
        }
        else if (columnName == SinkSearchColumnName && grid.Rows[e.RowIndex].Tag is RoomListItem sink)
        {
            OpenSinkSearch(sink);
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
        OpenGoogleSearch(query);
    }

    private static void OpenSinkSearch(RoomListItem item)
    {
        var parts = new[] { "sink_type", "model" }
            .Select(id => (item.Fields.TryGetValue(id, out var value) ? value.Text : null)?.Trim())
            .Where(text => !string.IsNullOrEmpty(text));
        var query = string.Join(" ", parts);
        if (query.Length == 0)
        {
            return;
        }

        OpenGoogleSearch(query);
    }

    private static void OpenGoogleSearch(string query)
    {
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
        AnswerInput.Set(item.Fields, itemField.Id, text, itemField.Type == QuestionType.Number);

        if (itemField.ColorPreview)
        {
            grid.InvalidateCell(grid.Columns[SwatchColumnPrefix + itemField.Id]!.Index, e.RowIndex);
        }

        if (itemField.CatalogLink)
        {
            grid.InvalidateCell(grid.Columns[ProductPageColumnPrefix + itemField.Id]!.Index, e.RowIndex);
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

    private void Grid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (e.Control is TextBox textBox)
        {
            // The grid reuses one TextBox for every text column, so only dimension columns keep the filter.
            DimensionInput.Detach(textBox);
            if (FieldAt(grid.CurrentCell?.ColumnIndex ?? -1)?.Type == QuestionType.Dimension)
            {
                DimensionInput.Attach(textBox);
            }
            return;
        }

        if (e.Control is not ComboBox comboBox)
        {
            return;
        }

        // The grid reuses one editing control across cells and columns, so reset what we change here.
        comboBox.SelectionChangeCommitted -= CatalogComboBox_SelectionChangeCommitted;
        comboBox.TextUpdate -= CatalogComboBox_TextUpdate;
        _catalogComboFilter?.Detach();
        _catalogComboFilter = null;

        if (CatalogFieldAt(grid.CurrentCell?.ColumnIndex ?? -1) is null)
        {
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.AutoCompleteMode = AutoCompleteMode.None;
            return;
        }

        // DropDown (not DropDownList) so values outside the catalog can still be typed.
        // Typed text narrows the list to options containing it; the cell's Items hold this row's full list.
        comboBox.DropDownStyle = ComboBoxStyle.DropDown;
        _catalogComboFilter = ComboBoxSubstringFilter.Attach(comboBox, () =>
            grid.CurrentCell is DataGridViewComboBoxCell cell ? cell.Items.Cast<object>().Select(o => o?.ToString() ?? "") : []);
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
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (ProductPageFieldIdAt(e.ColumnIndex) is { } linkFieldId)
        {
            // Only accessories with a link in the catalog show here, so every visible entry can be clicked.
            var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var url = ProductPageUrlAt(e.RowIndex, e.ColumnIndex);
            e.Value = url is null ? "" : grid.Rows[e.RowIndex].Cells[linkFieldId].Value as string;
            e.FormattingApplied = true;
            if (cell.ToolTipText != (url ?? ""))
            {
                cell.ToolTipText = url ?? "";
            }
            return;
        }

        if (grid.Columns[e.ColumnIndex].Tag is not string sourceFieldId)
        {
            return;
        }

        var hex = SwatchHexAt(e.RowIndex, sourceFieldId);
        e.Value = ColorMath.ToRgbLabel(hex);
        e.FormattingApplied = true;
        if (!SwatchColors.TryGet(hex, out var background, out var foreground))
        {
            return;
        }

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

        if (SwatchHexAt(e.RowIndex, sourceFieldId) is not { } hex)
        {
            return;
        }

        var rgb = ColorMath.ToRgbLabel(hex);
        var menu = new ContextMenuStrip();
        menu.Items.Add($"Copy RGB ({rgb})", null, (_, _) => ClipboardText.TrySet(FindForm(), rgb));
        menu.Items.Add($"Copy Hex ({hex})", null, (_, _) => ClipboardText.TrySet(FindForm(), hex));
        menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
        menu.Show(Cursor.Position);
    }

    /// <summary>The catalog hex (as "#RRGGBB") for the finish named in this row's source column, or null if none/unknown/invalid.</summary>
    private string? SwatchHexAt(int rowIndex, string sourceFieldId)
    {
        var name = grid.Rows[rowIndex].Cells[sourceFieldId].Value as string;
        return string.IsNullOrEmpty(name) ? null : ColorMath.ParseHexInput(_catalog!.HexFor(name));
    }

    /// <summary>The CatalogLink field a Product Page column belongs to, or null for any other column.</summary>
    private string? ProductPageFieldIdAt(int columnIndex) =>
        columnIndex >= 0 && grid.Columns[columnIndex].Name.StartsWith(ProductPageColumnPrefix, StringComparison.Ordinal)
            ? grid.Columns[columnIndex].Name[ProductPageColumnPrefix.Length..]
            : null;

    /// <summary>The web link of the accessory picked in this Product Page cell's row, or null if it isn't one, nothing known is picked, or it has no link.</summary>
    private string? ProductPageUrlAt(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || ProductPageFieldIdAt(columnIndex) is not { } fieldId ||
            grid.Rows[rowIndex].Cells[fieldId].Value is not string { Length: > 0 } label)
        {
            return null;
        }

        return _catalog!.UrlFor(label);
    }
}
