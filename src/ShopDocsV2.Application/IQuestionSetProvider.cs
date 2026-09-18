using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

public interface IQuestionSetProvider
{
    /// <summary>Loads the question schema from its remembered path, or from explicitPath if given. Throws QuestionSetLoadException on failure.</summary>
    Task<QuestionSet> LoadAsync(string? explicitPath = null, CancellationToken ct = default);
}

/// <summary>Wraps a question-schema load/parse failure so the UI can fall back to an OpenFileDialog, same spirit as the original app.</summary>
public sealed class QuestionSetLoadException : Exception
{
    public string? Path { get; }

    public QuestionSetLoadException(string? path, string message, Exception? inner = null)
        : base(message, inner)
    {
        Path = path;
    }
}
