using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using Moq;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AdminUsersControllerTests
{
    private UsersController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        var mockAuth = new Mock<IAuthService>();
        return new UsersController(mockAuth.Object, db);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsSeedUsers()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAllUsers(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminUserDto>>(ok.Value).ToList();
        Assert.True(list.Count >= 1);
        Assert.Contains(list, u => u.EmployeeNumber == "EMP001");
    }

    [Fact]
    public void GetRoles_ReturnsKnownRoles()
    {
        var controller = CreateController(out _);
        var result = controller.GetRoles();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<RoleOptionDto>>(ok.Value).ToList();
        Assert.Contains(list, r => r.Name == "Administrator" && r.Tier == 1.0m);
        Assert.Contains(list, r => r.Name == "Operator" && r.Tier == 6.0m);
    }

    [Fact]
    public async Task CreateUser_AddsNewUser()
    {
        var controller = CreateController(out var db);
        var siteId = TestHelpers.PlantPlt1Id;

        var dto = new CreateUserDto
        {
            EmployeeNumber = "NEW001",
            FirstName = "Jane",
            LastName = "Doe",
            DisplayName = "Jane Doe",
            RoleTier = 6.0m,
            RoleName = "Operator",
            DefaultSiteId = siteId,
            IsCertifiedWelder = false,
            RequirePinForLogin = false
        };

        var result = await controller.CreateUser(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminUserDto>(ok.Value);
        Assert.Equal("NEW001", created.EmployeeNumber);
        Assert.Equal("Jane Doe", created.DisplayName);
        Assert.True(db.Users.Any(u => u.EmployeeNumber == "NEW001"));
    }

    [Fact]
    public async Task UpdateUser_ModifiesExisting()
    {
        var controller = CreateController(out var db);
        var user = db.Users.First(u => u.EmployeeNumber == "EMP001");

        var dto = new UpdateUserDto
        {
            EmployeeNumber = user.EmployeeNumber,
            FirstName = "Updated",
            LastName = "Name",
            DisplayName = "Updated Name",
            RoleTier = 3.0m,
            RoleName = "Quality Manager",
            DefaultSiteId = TestHelpers.PlantPlt1Id,
            IsCertifiedWelder = true,
            RequirePinForLogin = true
        };

        var result = await controller.UpdateUser(user.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminUserDto>(ok.Value);
        Assert.Equal("Updated Name", updated.DisplayName);
        Assert.True(updated.IsCertifiedWelder);
    }

    [Fact]
    public async Task UpdateUser_ChangesEmployeeNumber()
    {
        var controller = CreateController(out var db);
        var user = db.Users.First(u => u.EmployeeNumber == "EMP001");

        var dto = new UpdateUserDto
        {
            EmployeeNumber = "EMP999",
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            RoleTier = user.RoleTier,
            RoleName = user.RoleName,
            DefaultSiteId = user.DefaultSiteId,
            IsCertifiedWelder = user.IsCertifiedWelder,
            RequirePinForLogin = user.RequirePinForLogin
        };

        var result = await controller.UpdateUser(user.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminUserDto>(ok.Value);
        Assert.Equal("EMP999", updated.EmployeeNumber);
        Assert.Equal("EMP999", db.Users.Single(u => u.Id == user.Id).EmployeeNumber);
    }

    [Fact]
    public async Task UpdateUser_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateUserDto { EmployeeNumber = "X", FirstName = "X", LastName = "X", DisplayName = "X", RoleTier = 6, RoleName = "Operator", DefaultSiteId = TestHelpers.PlantPlt1Id };
        var result = await controller.UpdateUser(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteUser_SoftDeletesSetsInactive()
    {
        var controller = CreateController(out var db);
        var newUser = new User { Id = Guid.NewGuid(), EmployeeNumber = "DEL001", FirstName = "Del", LastName = "Ete", DisplayName = "Del Ete", RoleTier = 6, RoleName = "Operator", DefaultSiteId = TestHelpers.PlantPlt1Id };
        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        var result = await controller.DeleteUser(newUser.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminUserDto>(ok.Value);
        Assert.False(dto.IsActive);
        var dbUser = db.Users.Single(u => u.Id == newUser.Id);
        Assert.False(dbUser.IsActive);
    }
}
