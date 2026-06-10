using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AskHR.Common.Dtos.Providers;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Providers;
using AskHR.Orchestrator.Extentions;

namespace AskHR.Orchestrator.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ProvidersAdminController : ControllerBase
{
    private readonly AgentDbContext _dbContext;

    public ProvidersAdminController(AgentDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet("metadata")]
    public async Task<IActionResult> GetProviderMetadatas()
    {
        var metadatas = await _dbContext.ProviderMetadatas
            .Select(p => new ProviderMetadataDto
            {
                Id = p.Id,
                Provider = p.Provider,
                ApprovalStatus = p.ApprovalStatus,
                DataResidency = p.DataResidency,
                Capabilities = p.Capabilities,
                SecretReference = p.SecretReference,
                HealthStatus = p.HealthStatus
            })
            .ToListAsync();
        return Ok(metadatas);
    }

    [HttpPost("metadata")]
    public async Task<IActionResult> CreateProviderMetadata([FromBody] ProviderMetadataDto dto)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedAccessException("User is not authenticated.");
        
        var metadata = new ProviderMetadata
        {
            Id = Guid.NewGuid(),
            Provider = dto.Provider ?? string.Empty,
            ApprovalStatus = dto.ApprovalStatus ?? string.Empty,
            DataResidency = dto.DataResidency ?? string.Empty,
            Capabilities = dto.Capabilities ?? new List<string>(),
            SecretReference = dto.SecretReference ?? string.Empty,
            HealthStatus = dto.HealthStatus ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        _dbContext.ProviderMetadatas.Add(metadata);
        await _dbContext.SaveChangesAsync();

        dto.Id = metadata.Id;
        return CreatedAtAction(nameof(GetProviderMetadatas), new { id = metadata.Id }, dto);
    }

    [HttpPut("metadata/{id}")]
    public async Task<IActionResult> UpdateProviderMetadata(Guid id, [FromBody] ProviderMetadataDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        
        var userId = User.GetUserId() ?? throw new UnauthorizedAccessException();
        var metadata = await _dbContext.ProviderMetadatas.FindAsync(id);
        
        if (metadata == null) return NotFound();

        metadata.Provider = dto.Provider ?? string.Empty;
        metadata.ApprovalStatus = dto.ApprovalStatus ?? string.Empty;
        metadata.DataResidency = dto.DataResidency ?? string.Empty;
        metadata.Capabilities = dto.Capabilities ?? new List<string>();
        metadata.SecretReference = dto.SecretReference ?? string.Empty;
        metadata.HealthStatus = dto.HealthStatus ?? string.Empty;
        metadata.UpdatedAt = DateTime.UtcNow;
        metadata.UpdatedByUserId = userId;

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("routes")]
    public async Task<IActionResult> GetModelRoutes()
    {
        var routes = await _dbContext.ModelRoutes
            .Select(r => new ModelRouteDto
            {
                Id = r.Id,
                Feature = r.Feature,
                PrimaryProvider = r.PrimaryProvider,
                PrimaryModel = r.PrimaryModel,
                Fallbacks = r.Fallbacks.Select(f => new FallbackRouteDto { Provider = f.Provider, Model = f.Model }).ToList(),
                RequiredCapabilities = r.RequiredCapabilities,
                DataPolicy = r.DataPolicy,
                Enabled = r.Enabled
            })
            .ToListAsync();
        return Ok(routes);
    }

    [HttpPost("routes")]
    public async Task<IActionResult> CreateModelRoute([FromBody] ModelRouteDto dto)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedAccessException();
        
        var route = new ModelRoute
        {
            Id = Guid.NewGuid(),
            Feature = dto.Feature ?? string.Empty,
            PrimaryProvider = dto.PrimaryProvider ?? string.Empty,
            PrimaryModel = dto.PrimaryModel ?? string.Empty,
            Fallbacks = (dto.Fallbacks ?? new List<FallbackRouteDto>())
                .Select(f => new FallbackRoute { Provider = f.Provider, Model = f.Model }).ToList(),
            RequiredCapabilities = dto.RequiredCapabilities ?? new List<string>(),
            DataPolicy = dto.DataPolicy ?? string.Empty,
            Enabled = dto.Enabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        _dbContext.ModelRoutes.Add(route);
        await _dbContext.SaveChangesAsync();

        dto.Id = route.Id;
        return CreatedAtAction(nameof(GetModelRoutes), new { id = route.Id }, dto);
    }

    [HttpPut("routes/{id}")]
    public async Task<IActionResult> UpdateModelRoute(Guid id, [FromBody] ModelRouteDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");
        
        var userId = User.GetUserId() ?? throw new UnauthorizedAccessException();
        var route = await _dbContext.ModelRoutes.FindAsync(id);
        
        if (route == null) return NotFound();

        route.Feature = dto.Feature ?? string.Empty;
        route.PrimaryProvider = dto.PrimaryProvider ?? string.Empty;
        route.PrimaryModel = dto.PrimaryModel ?? string.Empty;
        route.Fallbacks = (dto.Fallbacks ?? new List<FallbackRouteDto>())
            .Select(f => new FallbackRoute { Provider = f.Provider, Model = f.Model }).ToList();
        route.RequiredCapabilities = dto.RequiredCapabilities ?? new List<string>();
        route.DataPolicy = dto.DataPolicy ?? string.Empty;
        route.Enabled = dto.Enabled;
        route.UpdatedAt = DateTime.UtcNow;
        route.UpdatedByUserId = userId;

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}
