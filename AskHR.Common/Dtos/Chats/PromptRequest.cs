using AskHR.Common.Dtos.Upload;

namespace AskHR.Common.Dtos.Chats;

public record PromptRequest<TUpload>(
    string Prompt,
    Guid ConversationId,
    string? SessionId,
    IEnumerable<TUpload>? Documents,
    Guid AgentId
)
where TUpload : UploadItem;