using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

/// <summary>Builds a .ordx XML interchange file for the shop's CV cabinet-design software.</summary>
public interface IOrdxExportService
{
    string BuildOrdxXml(Job job, QuestionSet questionSet, DateTime? createdAt = null);
}
