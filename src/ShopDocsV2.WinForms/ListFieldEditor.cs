using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

/// <summary>
/// Renders a List question as an editable grid: one column per itemField, one row per RoomListItem.
/// Catalog-backed columns get free-typing autocomplete (via EditingControlShowing); plain-select
/// columns get a real constrained dropdown; a ColorPreview column gets a swatch background.
/// </summary>
public partial class ListFieldEditor : UserControl
{
    private const string RemoveColumnName = "__remove";
    private const string SpecsColumnName = "__specs";

    private Room? _room;
    private QuestionDef? _question;
    private CatalogSnapshot? _catalog;
    private Action? _onAnswerChanged;
    private List<RoomListItem>? _items;

    public ListFieldEditor()
    {
        InitializeComponent();

        grid.DataError += (_, e) => e.ThrowException = false;
        grid.CellContentClick += Grid_CellContentClick;
        grid.CellValueChanged += Grid_CellValueChanged;
        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (grid.IsCurrentCellDirty)
            {
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        grid.EditingControlShowing += Grid_EditingControlShowing;
        grid.CellFormatting += Grid_CellFormatting;
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
            else
            {
                column = new DataGridViewTextBoxColumn { Name = field.Id, HeaderText = field.Label };
            }
            grid.Columns.Add(column);
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
            row.Cells[field.Id].Value = item.Fields.TryGetValue(field.Id, out var value) ? value.DisplayText : "";
        }
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

        foreach (var dependent in _question.ItemFields!.Where(f => f.CatalogFilterBy == itemField.Id))
        {
            if (!item.Fields.TryGetValue(dependent.Id, out var depValue))
            {
                continue;
            }

            var validOptions = _catalog!.Resolve(dependent.CatalogSource, text);
            if (validOptions.Contains(depValue.Text))
            {
                continue;
            }

            item.Fields.Remove(dependent.Id);
            var depColumnIndex = grid.Columns[dependent.Id]!.Index;
            grid.Rows[e.RowIndex].Cells[depColumnIndex].Value = "";
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
        var columnIndex = grid.CurrentCell?.ColumnIndex ?? -1;
        if (columnIndex < 0 || e.Control is not TextBox textBox)
        {
            return;
        }

        var field = _question!.ItemFields?.FirstOrDefault(f => f.Id == grid.Columns[columnIndex].Name);
        if (field is null || string.IsNullOrEmpty(field.CatalogSource))
        {
            return;
        }

        string? filterValue = null;
        if (field.CatalogFilterBy is not null && grid.CurrentCell?.OwningRow?.Tag is RoomListItem item &&
            item.Fields.TryGetValue(field.CatalogFilterBy, out var fv))
        {
            filterValue = fv.Text;
        }

        var options = _catalog!.Resolve(field.CatalogSource, filterValue);

        textBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
        var source = new AutoCompleteStringCollection();
        source.AddRange([.. options]);
        textBox.AutoCompleteCustomSource = source;
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        var field = _question!.ItemFields?.FirstOrDefault(f => f.Id == grid.Columns[e.ColumnIndex].Name);
        if (field is not { ColorPreview: true })
        {
            return;
        }

        var text = e.Value as string;
        var hex = string.IsNullOrEmpty(text) ? null : _catalog!.HexFor(text);
        if (hex is null)
        {
            return;
        }

        var normalizedHex = hex.StartsWith('#') ? hex : "#" + hex;
        e.CellStyle!.BackColor = ColorTranslator.FromHtml(normalizedHex);
        e.CellStyle.ForeColor = ColorTranslator.FromHtml(ColorMath.GetContrastingTextColor(hex));
    }
}
