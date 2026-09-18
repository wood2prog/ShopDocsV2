using ShopDocsV2.Domain;

namespace ShopDocsV2.Application.Tests;

/// <summary>Small fixture builders mirroring the shape of the real questions.json used by the shop.</summary>
internal static class TestFixtures
{
    public static QuestionSet BuildQuestionSet()
    {
        QuestionDef Field(string id, string label, QuestionType type = QuestionType.Text) =>
            new() { Id = id, Label = label, Type = type };

        return new QuestionSet
        {
            Sections = new List<QuestionSection>
            {
                new()
                {
                    Title = "Cabinets",
                    Questions = new List<QuestionDef>
                    {
                        new() { Id = "cabinet_style", Label = "Cabinet Style", Type = QuestionType.Select, Options = new() { "Framed", "Frameless" } },
                        new() { Id = "cabinet_door_style", Label = "Door Style", Type = QuestionType.Select, Options = new() { "Shaker", "Raised Panel" } },
                        new() { Id = "soft_close", Label = "Soft-close hinges/slides", Type = QuestionType.Checkbox },
                        new()
                        {
                            Id = "cabinet_finishes", Label = "Cabinet Finishes", Type = QuestionType.List, AddLabel = "+ Add Cabinet Finish",
                            ItemFields = new List<QuestionDef> { Field("name", "Name"), Field("wood", "Wood"), Field("finish", "Finish") }
                        },
                        new()
                        {
                            Id = "cabinet_hardware", Label = "Hardware (pulls, knobs)", Type = QuestionType.List, AddLabel = "+ Add Hardware",
                            ItemFields = new List<QuestionDef> { Field("name", "Name"), Field("quantity", "Qty"), Field("catalogItem", "Catalog Item"), Field("color", "Color"), Field("model", "Model Number") }
                        }
                    }
                },
                new()
                {
                    Title = "Countertops",
                    Questions = new List<QuestionDef>
                    {
                        new()
                        {
                            Id = "countertops", Label = "Countertops", Type = QuestionType.List, AddLabel = "+ Add Countertop",
                            ItemFields = new List<QuestionDef> { Field("material", "Material"), Field("color", "Color / Pattern"), Field("edge_profile", "Edge Profile"), Field("backsplash", "Backsplash") }
                        }
                    }
                },
                new()
                {
                    Title = "Sink & Faucet",
                    Questions = new List<QuestionDef>
                    {
                        new()
                        {
                            Id = "sinks", Label = "Sinks", Type = QuestionType.List, AddLabel = "+ Add Sink",
                            ItemFields = new List<QuestionDef> { Field("sink_type", "Sink Type"), Field("model", "Model Number"), Field("faucet_style", "Faucet Style") }
                        }
                    }
                },
                new()
                {
                    Title = "Appliances",
                    Questions = new List<QuestionDef>
                    {
                        new()
                        {
                            Id = "appliance_package", Label = "Appliances", Type = QuestionType.List, AddLabel = "+ Add Appliance",
                            ItemFields = new List<QuestionDef> { Field("name", "Appliance"), Field("model", "Model Number") }
                        }
                    }
                },
                new()
                {
                    Title = "Additional Notes",
                    Questions = new List<QuestionDef>
                    {
                        new() { Id = "notes", Label = "Notes", Type = QuestionType.List, AddLabel = "+ Add Note", ItemFields = new List<QuestionDef> { Field("text", "Note") } }
                    }
                }
            }
        };
    }

    public static RoomListItem ListItem(int sortOrder, params (string FieldId, string Value)[] fields)
    {
        var item = new RoomListItem { Id = Guid.NewGuid(), SortOrder = sortOrder };
        foreach (var (fieldId, value) in fields) item.Fields[fieldId] = AnswerValue.Of(value);
        return item;
    }
}
