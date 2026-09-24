using System.Globalization;
using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

/// <summary>Moves typed input between editor controls and an answer dictionary (Room.Answers or RoomListItem.Fields).</summary>
internal static class AnswerInput
{
    /// <summary>Stores typed text under id. Empty text removes the answer; a number field keeps only a value that parses, and removes it otherwise.</summary>
    public static void Set(Dictionary<string, AnswerValue> answers, string id, string text, bool isNumber)
    {
        if (isNumber)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                answers[id] = AnswerValue.Of(number);
            }
            else
            {
                answers.Remove(id);
            }
        }
        else if (string.IsNullOrEmpty(text))
        {
            answers.Remove(id);
        }
        else
        {
            answers[id] = AnswerValue.Of(text);
        }
    }

    /// <summary>The answer's display text, or null if there's no answer.</summary>
    public static string? GetText(Dictionary<string, AnswerValue> answers, string id) =>
        answers.TryGetValue(id, out var value) ? value.DisplayText : null;
}
