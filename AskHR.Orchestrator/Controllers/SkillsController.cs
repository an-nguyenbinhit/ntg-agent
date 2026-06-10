using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Agents;
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
        return Ok(skills);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSkill(Guid id, CancellationToken ct)
    {
        var skill = await _context.Skills.FindAsync(new object[] { id }, ct);
        if (skill == null) return NotFound();
        return Ok(skill);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSkill([FromBody] Skill skill, CancellationToken ct)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedAccessException();
        
        skill.Id = Guid.NewGuid();
        skill.CreatedAt = DateTime.UtcNow;
        skill.UpdatedAt = DateTime.UtcNow;
        skill.UpdatedByUserId = userId;

        _context.Skills.Add(skill);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, skill);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSkill(Guid id, [FromBody] Skill updatedSkill, CancellationToken ct)
    {
        if (id != updatedSkill.Id) return BadRequest();

        var userId = User.GetUserId() ?? throw new UnauthorizedAccessException();

        var skill = await _context.Skills.FindAsync(new object[] { id }, ct);
        if (skill == null) return NotFound();

        skill.SkillId = updatedSkill.SkillId;
        skill.Name = updatedSkill.Name;
        skill.Description = updatedSkill.Description;
        skill.Enabled = updatedSkill.Enabled;
        skill.Owner = updatedSkill.Owner;
        skill.ApprovalStatus = updatedSkill.ApprovalStatus;
        skill.Version = updatedSkill.Version;
        skill.Scope = updatedSkill.Scope;
        skill.Instructions = updatedSkill.Instructions;
        skill.AnswerPolicy = updatedSkill.AnswerPolicy;
        skill.Tools = updatedSkill.Tools;
        skill.Attachments = updatedSkill.Attachments;
        skill.Escalation = updatedSkill.Escalation;

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
}
