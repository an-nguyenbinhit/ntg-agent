using Microsoft.AspNetCore.Components.Forms;
using AskHR.Common.Dtos.Chats;
using AskHR.Common.Dtos.Upload;

namespace AskHR.Orchestrator.Dtos;

public class UploadItemForm : UploadItem
{
    public IFormFile? Content { get; set; }
}

public record PromptRequestForm(string Prompt, Guid ConversationId, string? SessionId, IEnumerable<UploadItemForm>? Documents, Guid AgentId) : PromptRequest<UploadItemForm>(Prompt, ConversationId, SessionId, Documents, AgentId);