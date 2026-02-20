using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Tests;

public class KanbanCardsControllerTests
{
    private KanbanCardsController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        return new KanbanCardsController(db);
    }

    [Fact]
    public async Task GetAll_ReturnsSeedCards()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminBarcodeCardDto>>(ok.Value).ToList();
        Assert.True(list.Count >= 5);
        Assert.Contains(list, c => c.CardValue == "01" && c.Color == "Red");
    }

    [Fact]
    public async Task Create_AddsCard()
    {
        var controller = CreateController(out var db);
        var dto = new CreateBarcodeCardDto { CardValue = "99", Color = "Pink", Description = "Test card" };

        var result = await controller.Create(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminBarcodeCardDto>(ok.Value);
        Assert.Equal("99", created.CardValue);
        Assert.Equal("Pink", created.Color);
        Assert.True(db.BarcodeCards.Any(c => c.CardValue == "99"));
    }

    [Fact]
    public async Task Delete_RemovesCard()
    {
        var controller = CreateController(out var db);
        var card = new BarcodeCard { Id = Guid.NewGuid(), CardValue = "DEL", Color = "Gray" };
        db.BarcodeCards.Add(card);
        await db.SaveChangesAsync();

        var result = await controller.Delete(card.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.False(db.BarcodeCards.Any(c => c.Id == card.Id));
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }
}
