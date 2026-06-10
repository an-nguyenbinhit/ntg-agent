using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Agents;
using AskHR.Common.Dtos.Agents;
using AskHR.Orchestrator.Extentions;

namespace AskHR.Orchestrator.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class SkillsController : ControllerBase
{
    private readonly AgentDbContext _context;

    public SkillsController(AgentDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSkills(CancellationToken ct)
    {
        var skills = await _context.Skills.ToListAsync(ct);
        var dtos = skills.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSkill(Guid id, CancellationToken ct)
    {
        var skill = await _context.Skills.FindAsync(new object[] { id }, ct);
        if (skill == null) return NotFound();
        return Ok(MapToDto(skill));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSkill([FromBody] SkillDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedAccessException();
        
        var skill = new Skill();
        MapToEntity(dto, skill);
        skill.Id = Guid.NewGuid();
        skill.CreatedAt = DateTime.UtcNow;
        skill.UpdatedAt = DateTime.UtcNow;
        skill.UpdatedByUserId = userId;

        _context.Skills.Add(skill);
        await _context.SaveChangesAsync(ct);

        dto.Id = skill.Id;
        return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSkill(Guid id, [FromBody] SkillDto updatedSkill, CancellationToken ct)
    {
        if (id != updatedSkill.Id) return BadRequest();

        var userId = User.GetUserId() ?? throw new UnauthorizedAccessException();

        var skill = await _context.Skills.FindAsync(new object[] { id }, ct);
        if (skill == null) return NotFound();

        MapToEntity(updatedSkill, skill);

        skill.UpdatedAt = DateTime.UtcNow;
        skill.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSkill(Guid id, CancellationToken ct)
    {
        var skill = await _context.Skills.FindAsync(new object[] { id }, ct);
        if (skill == null) return NotFound();

        _context.Skills.Remove(skill);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }

    private static SkillDto MapToDto(Skill skill)
    {
        return new SkillDto
        {
            Id = skill.Id,
            SkillId = skill.SkillId,
            Name = skill.Name,
            Description = skill.Description,
            Enabled = skill.Enabled,
            Owner = skill.Owner,
            ApprovalStatus = skill.ApprovalStatus,
            Version = skill.Version,
            Instructions = skill.Instructions,
            Scope = new SkillScopeDto
            {
                Topics = skill.Scope.Topics,
                Tags = skill.Scope.Tags,
                BusinessUnits = skill.Scope.BusinessUnits
            },
            AnswerPolicy = new SkillAnswerPolicyDto
            {
                RequireCitation = skill.AnswerPolicy.RequireCitation,
                RefuseIfExpired = skill.AnswerPolicy.RefuseIfExpired,
                ClarifyingQuestions = skill.AnswerPolicy.ClarifyingQuestions
            },
            Tools = skill.Tools,
            Attachments = skill.Attachments,
            Escalation = new SkillEscalationDto
            {
                FallbackContact = skill.Escalation.FallbackContact,
                SeverityHint = skill.Escalation.SeverityHint
            },
            PrimaryProvider = skill.PrimaryProvider,
            PrimaryModel = skill.PrimaryModel,
            CreatedAt = skill.CreatedAt,
            UpdatedAt = skill.UpdatedAt
        };
    }

    private static void MapToEntity(SkillDto dto, Skill skill)
    {
        skill.SkillId = dto.SkillId;
        skill.Name = dto.Name;
        skill.Description = dto.Description;
        skill.Enabled = dto.Enabled;
        skill.Owner = dto.Owner;
        skill.ApprovalStatus = dto.ApprovalStatus;
        skill.Version = dto.Version;
        skill.Instructions = dto.Instructions;
        skill.PrimaryProvider = dto.PrimaryProvider;
        skill.PrimaryModel = dto.PrimaryModel;
        
        skill.Scope.Topics = dto.Scope.Topics;
        skill.Scope.Tags = dto.Scope.Tags;
        skill.Scope.BusinessUnits = dto.Scope.BusinessUnits;

        skill.AnswerPolicy.RequireCitation = dto.AnswerPolicy.RequireCitation;
        skill.AnswerPolicy.RefuseIfExpired = dto.AnswerPolicy.RefuseIfExpired;
        skill.AnswerPolicy.ClarifyingQuestions = dto.AnswerPolicy.ClarifyingQuestions;

        skill.Tools = dto.Tools;
        skill.Attachments = dto.Attachments;

        skill.Escalation.FallbackContact = dto.Escalation.FallbackContact;
        skill.Escalation.SeverityHint = dto.Escalation.SeverityHint;
    }
}
