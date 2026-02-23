using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task GetAllUsers_ReturnsSeedUsers()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);

        var result = await sut.GetAllUsersAsync();

        Assert.True(result.Count >= 1);
        Assert.Contains(result, u => u.EmployeeNumber == "EMP001");
    }

    [Fact]
    public async Task CreateUser_Succeeds()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);

        var result = await sut.CreateUserAsync(new CreateUserDto
        {
            EmployeeNumber = "SVC001",
            FirstName = "Service",
            LastName = "Test",
            DisplayName = "Service Test",
            RoleTier = 6.0m,
            RoleName = "Operator",
            DefaultSiteId = TestHelpers.PlantPlt1Id,
        });

        Assert.Equal("SVC001", result.EmployeeNumber);
        Assert.True(db.Users.Any(u => u.EmployeeNumber == "SVC001"));
    }

    [Fact]
    public async Task CreateUser_DuplicateEmpNo_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateUserAsync(new CreateUserDto
            {
                EmployeeNumber = "EMP001",
                FirstName = "Dup",
                LastName = "Test",
                DisplayName = "Dup",
                RoleTier = 6,
                RoleName = "Operator",
                DefaultSiteId = TestHelpers.PlantPlt1Id,
            }));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateUser_InvalidPin_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateUserAsync(new CreateUserDto
            {
                EmployeeNumber = "PIN01",
                FirstName = "P",
                LastName = "T",
                DisplayName = "P T",
                RoleTier = 6,
                RoleName = "Operator",
                DefaultSiteId = TestHelpers.PlantPlt1Id,
                Pin = "ab"
            }));
    }

    [Fact]
    public async Task CreateUser_WithValidPin_HashesPin()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);

        var result = await sut.CreateUserAsync(new CreateUserDto
        {
            EmployeeNumber = "PINOK1",
            FirstName = "P",
            LastName = "T",
            DisplayName = "P T",
            RoleTier = 6,
            RoleName = "Operator",
            DefaultSiteId = TestHelpers.PlantPlt1Id,
            Pin = "1234"
        });

        Assert.True(result.HasPin);
        var user = db.Users.Single(u => u.EmployeeNumber == "PINOK1");
        Assert.NotNull(user.PinHash);
    }

    [Fact]
    public async Task CreateUser_AuthorizedInspector_NormalizesEmpNo()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);

        var result = await sut.CreateUserAsync(new CreateUserDto
        {
            EmployeeNumber = "123",
            FirstName = "AI",
            LastName = "User",
            DisplayName = "AI User",
            RoleTier = 5.5m,
            RoleName = "Authorized Inspector",
            DefaultSiteId = TestHelpers.PlantPlt1Id,
            UserType = (int)UserType.AuthorizedInspector
        });

        Assert.Equal("AI123", result.EmployeeNumber);
    }

    [Fact]
    public async Task UpdateUser_NotFound_ReturnsNull()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);

        var result = await sut.UpdateUserAsync(Guid.NewGuid(), new UpdateUserDto
        {
            EmployeeNumber = "X", FirstName = "X", LastName = "X", DisplayName = "X",
            RoleTier = 6, RoleName = "Operator", DefaultSiteId = TestHelpers.PlantPlt1Id
        }, null);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUser_Forbidden_WhenLowTierCallerEditsHighTierUser()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);
        var user = db.Users.First(u => u.EmployeeNumber == "EMP001");
        user.RoleTier = 1;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.UpdateUserAsync(user.Id, new UpdateUserDto
            {
                EmployeeNumber = user.EmployeeNumber, FirstName = "X", LastName = "X",
                DisplayName = "X", RoleTier = 1, RoleName = "Administrator",
                DefaultSiteId = TestHelpers.PlantPlt1Id
            }, callerRoleTier: 3));
    }

    [Fact]
    public async Task UpdateUser_ClearsPin_WhenRequirePinDisabled()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);
        var user = db.Users.First(u => u.EmployeeNumber == "EMP001");
        user.PinHash = "somehash";
        await db.SaveChangesAsync();

        var result = await sut.UpdateUserAsync(user.Id, new UpdateUserDto
        {
            EmployeeNumber = user.EmployeeNumber, FirstName = user.FirstName, LastName = user.LastName,
            DisplayName = user.DisplayName, RoleTier = user.RoleTier, RoleName = user.RoleName,
            DefaultSiteId = user.DefaultSiteId, RequirePinForLogin = false
        }, null);

        Assert.NotNull(result);
        Assert.False(result!.HasPin);
        Assert.Null(db.Users.Single(u => u.Id == user.Id).PinHash);
    }

    [Fact]
    public async Task DeleteUser_SoftDeletes()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);
        var newUser = new User { Id = Guid.NewGuid(), EmployeeNumber = "DEL002", FirstName = "D", LastName = "U", DisplayName = "D U", RoleTier = 6, RoleName = "Operator", DefaultSiteId = TestHelpers.PlantPlt1Id };
        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        var result = await sut.DeleteUserAsync(newUser.Id, null);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        Assert.False(db.Users.Single(u => u.Id == newUser.Id).IsActive);
    }

    [Fact]
    public async Task DeleteUser_NotFound_ReturnsNull()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);

        var result = await sut.DeleteUserAsync(Guid.NewGuid(), null);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteUser_Forbidden_WhenLowTierCallerDeletesHighTierUser()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new UserService(db);
        var user = db.Users.First(u => u.EmployeeNumber == "EMP001");
        user.RoleTier = 2;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.DeleteUserAsync(user.Id, callerRoleTier: 3));
    }
}
