using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

/// <summary>Builds a room tab's dynamic contents from a QuestionSet + the loaded catalog data.</summary>
internal static class RoomFormBuilder
{
    private const string SkipOptionText = "-- Skip --";
    private const int ListEditorHeight = 220;

    public static Control Build(Room room, QuestionSet questionSet, CatalogSnapshot catalog, Action onAnswerChanged)
    {
        var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        var sectionsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8)
        };
        sectionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var row = 0;
        foreach (var section in questionSet.Sections)
        {
            var scalarQuestions = section.Questions.Where(q => q.Type != QuestionType.List).ToList();
            var listQuestions = section.Questions.Where(q => q.Type == QuestionType.List).ToList();

            if (scalarQuestions.Count > 0)
            {
                sectionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                var groupBox = BuildScalarGroupBox(room, section.Title, scalarQuestions, catalog, onAnswerChanged);
                groupBox.Dock = DockStyle.Top;
                groupBox.Margin = new Padding(0, 0, 0, 8);
                sectionsPanel.Controls.Add(groupBox, 0, row++);
            }

            foreach (var listQuestion in listQuestions)
            {
                sectionsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, ListEditorHeight + 30));
                var groupBox = BuildListGroupBox(room, listQuestion, catalog, onAnswerChanged);
                groupBox.Dock = DockStyle.Top;
                groupBox.Margin = new Padding(0, 0, 0, 8);
                sectionsPanel.Controls.Add(groupBox, 0, row++);
            }
        }

        if (row == 0)
        {
            sectionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sectionsPanel.Controls.Add(new Label
            {
                Text = "This room has no fields yet.",
                AutoSize = true,
                ForeColor = SystemColors.GrayText
            }, 0, 0);
        }

        scrollPanel.Controls.Add(sectionsPanel);
        return scrollPanel;
    }

    private static GroupBox BuildListGroupBox(Room room, QuestionDef question, CatalogSnapshot catalog, Action onAnswerChanged)
    {
        var editor = new ListFieldEditor { Dock = DockStyle.Fill, Height = ListEditorHeight };
        editor.Bind(room, question, catalog, onAnswerChanged);

        var groupBox = new GroupBox
        {
            Text = question.Label,
            Height = ListEditorHeight + 30,
            Padding = new Padding(4, 20, 4, 4)
        };
        groupBox.Controls.Add(editor);
        return groupBox;
    }

    private static GroupBox BuildScalarGroupBox(Room room, string title, List<QuestionDef> questions, CatalogSnapshot catalog, Action onAnswerChanged)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8, 20, 8, 8)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (var i = 0; i < questions.Count; i++)
        {
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            AddQuestionRow(table, i, room, questions[i], catalog, onAnswerChanged);
        }

        var groupBox = new GroupBox
        {
            Text = title,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        groupBox.Controls.Add(table);
        return groupBox;
    }

    private static void AddQuestionRow(TableLayoutPanel table, int row, Room room, QuestionDef question, CatalogSnapshot catalog, Action onAnswerChanged)
    {
        if (question.Type == QuestionType.Checkbox)
        {
            var checkBox = BuildCheckBox(room, question, onAnswerChanged);
            checkBox.Anchor = AnchorStyles.Left;
            checkBox.Margin = new Padding(3, 6, 3, 3);
            table.Controls.Add(checkBox, 1, row);
            return;
        }

        table.Controls.Add(new Label
        {
            Text = question.Label,
            AutoSize = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 6, 3, 3),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        Control input = question.Type switch
        {
            QuestionType.Textarea => BuildTextArea(room, question, onAnswerChanged),
            QuestionType.Select when !string.IsNullOrEmpty(question.CatalogSource) => BuildCatalogSelect(room, question, catalog, onAnswerChanged),
            QuestionType.Select => BuildSelect(room, question, onAnswerChanged),
            QuestionType.Number => BuildTextBox(room, question, numericOnly: true, onAnswerChanged),
            _ => BuildTextBox(room, question, numericOnly: false, onAnswerChanged)
        };
        input.Dock = DockStyle.Fill;
        table.Controls.Add(input, 1, row);
    }

    private static CheckBox BuildCheckBox(Room room, QuestionDef question, Action onAnswerChanged)
    {
        var checkBox = new CheckBox
        {
            Text = question.Label,
            AutoSize = true,
            Checked = room.Answers.TryGetValue(question.Id, out var value) && value is { Kind: AnswerValueKind.Bool, Bool: true }
        };
        checkBox.CheckedChanged += (_, _) =>
        {
            if (checkBox.Checked)
            {
                room.Answers[question.Id] = AnswerValue.Of(true);
            }
            else
            {
                room.Answers.Remove(question.Id);
            }
            onAnswerChanged();
        };
        return checkBox;
    }

    private static TextBox BuildTextArea(Room room, QuestionDef question, Action onAnswerChanged)
    {
        var textBox = new TextBox
        {
            Text = AnswerInput.GetText(room.Answers, question.Id) ?? "",
            PlaceholderText = question.Placeholder ?? "",
            Multiline = true,
            Height = 60,
            ScrollBars = ScrollBars.Vertical
        };
        textBox.TextChanged += (_, _) =>
        {
            AnswerInput.Set(room.Answers, question.Id, textBox.Text, isNumber: false);
            onAnswerChanged();
        };
        return textBox;
    }

    private static TextBox BuildTextBox(Room room, QuestionDef question, bool numericOnly, Action onAnswerChanged)
    {
        var textBox = new TextBox
        {
            Text = AnswerInput.GetText(room.Answers, question.Id) ?? "",
            PlaceholderText = question.Placeholder ?? ""
        };

        if (numericOnly)
        {
            textBox.KeyPress += (_, e) =>
            {
                if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar))
                {
                    return;
                }
                if (e.KeyChar == '.' && !textBox.Text.Contains('.'))
                {
                    return;
                }
                if (e.KeyChar == '-' && textBox.SelectionStart == 0 && !textBox.Text.Contains('-'))
                {
                    return;
                }
                e.Handled = true;
            };
        }

        textBox.TextChanged += (_, _) =>
        {
            AnswerInput.Set(room.Answers, question.Id, textBox.Text, numericOnly);
            onAnswerChanged();
        };

        return textBox;
    }

    private static ComboBox BuildSelect(Room room, QuestionDef question, Action onAnswerChanged)
    {
        var comboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        comboBox.Items.Add(SkipOptionText);
        foreach (var option in question.Options ?? [])
        {
            comboBox.Items.Add(option);
        }

        var existing = AnswerInput.GetText(room.Answers, question.Id);
        if (existing is not null)
        {
            comboBox.SelectedItem = existing;
        }
        else if (question.Default is not null)
        {
            // Matches the original app: a default is committed into the answer immediately, not just displayed.
            comboBox.SelectedItem = question.Default;
            room.Answers[question.Id] = AnswerValue.Of(question.Default);
        }
        else
        {
            comboBox.SelectedIndex = 0;
        }

        comboBox.SelectedIndexChanged += (_, _) =>
        {
            var selected = comboBox.SelectedItem as string;
            if (selected is null || selected == SkipOptionText)
            {
                room.Answers.Remove(question.Id);
            }
            else
            {
                room.Answers[question.Id] = AnswerValue.Of(selected);
            }
            onAnswerChanged();
        };

        return comboBox;
    }

    private static CatalogAutocompleteComboBox BuildCatalogSelect(Room room, QuestionDef question, CatalogSnapshot catalog, Action onAnswerChanged)
    {
        var comboBox = new CatalogAutocompleteComboBox();
        var existing = AnswerInput.GetText(room.Answers, question.Id);
        comboBox.RefreshOptions(catalog.Resolve(question.CatalogSource, null), existing);

        comboBox.TextChanged += (_, _) =>
        {
            AnswerInput.Set(room.Answers, question.Id, comboBox.Text, isNumber: false);
            onAnswerChanged();
        };

        return comboBox;
    }
}
