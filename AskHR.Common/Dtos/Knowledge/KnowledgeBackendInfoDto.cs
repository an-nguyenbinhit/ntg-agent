namespace AskHR.Common.Dtos.Knowledge;

/// <summary>
/// Read-only snapshot of the knowledge (RAG) backend currently wired into the
/// Kernel Memory service. Surfaced in the Admin portal so operators can see which
/// vector store / embedding model is active without being able to switch it at
/// runtime (the store is bound at service startup, not per agent).
/// </summary>
public record KnowledgeBackendInfoDto
{
    /// <summary>Friendly name of the active vector store, e.g. "SqlServer", "AzureAISearch".</summary>
    public string MemoryDb { get; init; } = "Unknown";

    /// <summary>Embedding model used to index/query documents.</summary>
    public string EmbeddingModel { get; init; } = "Unknown";

    /// <summary>Text generation model backing retrieval answers.</summary>
    public string TextModel { get; init; } = "Unknown";

    /// <summary>Whether the knowledge service responded successfully to the probe.</summary>
    public bool Healthy { get; init; }
}
