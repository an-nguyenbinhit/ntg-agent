using Microsoft.AspNetCore.Http;

namespace AskHR.Orchestrator.Services.Slack;

public interface ISlackRequestVerifier
{
    bool Verify(HttpRequest request, string rawBody);
}
