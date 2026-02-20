using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/kanban-cards")]
public class KanbanCardsController : ControllerBase
{
    private readonly MesDbContext _db;

    public KanbanCardsController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminBarcodeCardDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _db.BarcodeCards
            .OrderBy(b => b.CardValue)
            .Select(b => new AdminBarcodeCardDto
            {
                Id = b.Id,
                CardValue = b.CardValue,
                Color = b.Color,
                Description = b.Description
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AdminBarcodeCardDto>> Create([FromBody] CreateBarcodeCardDto dto, CancellationToken cancellationToken)
    {
        var card = new BarcodeCard
        {
            Id = Guid.NewGuid(),
            CardValue = dto.CardValue,
            Color = dto.Color,
            Description = dto.Description
        };
        _db.BarcodeCards.Add(card);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new AdminBarcodeCardDto { Id = card.Id, CardValue = card.CardValue, Color = card.Color, Description = card.Description });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var card = await _db.BarcodeCards.FindAsync(new object[] { id }, cancellationToken);
        if (card == null) return NotFound();
        _db.BarcodeCards.Remove(card);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
