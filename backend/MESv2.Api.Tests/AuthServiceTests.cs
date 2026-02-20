using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AuthServiceTests
{
    private static IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "dev-secret-key-min-32-chars-long-for-hs256",
                ["Jwt:Issuer"] = "MESv2",
                ["Jwt:Audience"] = "MESv2"
            })
            .Build();
    }

    [Fact]
    public async Task GetLoginConfig_ReturnsConfig_WhenUserExists()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var config = CreateConfig();
        var sut = new AuthService(db, config);

        var result = await sut.GetLoginConfigAsync("EMP001");

        Assert.NotNull(result);
        Assert.False(result.RequiresPin);
        Assert.Equal(TestHelpers.PlantPlt1Id, result.DefaultSiteId);
        Assert.True(result.AllowSiteSelection);
        Assert.False(result.IsWelder);
        Assert.Equal("Jeff Thompson", result.UserName);
    }

    [Fact]
    public async Task GetLoginConfig_ReturnsNull_WhenUserNotFound()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var config = CreateConfig();
        var sut = new AuthService(db, config);

        var result = await sut.GetLoginConfigAsync("NONEXISTENT");

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_ReturnsTokenAndUser_WhenValid()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var config = CreateConfig();
        var sut = new AuthService(db, config);

        var result = await sut.LoginAsync("EMP001", null, TestHelpers.PlantPlt1Id, false);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.NotNull(result.User);
        Assert.Equal(TestHelpers.TestUserId, result.User.Id);
        Assert.Equal("EMP001", result.User.EmployeeNumber);
        Assert.Equal("Jeff Thompson", result.User.DisplayName);
        Assert.Equal(TestHelpers.PlantPlt1Id, result.User.DefaultSiteId);
    }

    [Fact]
    public async Task Login_ReturnsPlantTimeZoneId_ForUserPlant()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var config = CreateConfig();
        var sut = new AuthService(db, config);

        var result = await sut.LoginAsync("EMP001", null, TestHelpers.PlantPlt1Id, false);

        Assert.NotNull(result);
        Assert.Equal("America/Chicago", result.User.PlantTimeZoneId);
    }

    [Fact]
    public async Task Login_ReturnsCorrectTimeZone_ForEachPlant()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var config = CreateConfig();
        var sut = new AuthService(db, config);

        var fremont = await db.Users.FirstAsync(u => u.EmployeeNumber == "EMP004");
        var result = await sut.LoginAsync("EMP004", null, fremont.DefaultSiteId, false);

        Assert.NotNull(result);
        Assert.Equal("America/New_York", result.User.PlantTimeZoneId);
    }

    [Fact]
    public async Task Login_ReturnsNull_WhenPinRequired_AndMissing()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var user = await db.Users.FirstAsync(u => u.EmployeeNumber == "EMP001");
        user.RequirePinForLogin = true;
        await db.SaveChangesAsync();

        var config = CreateConfig();
        var sut = new AuthService(db, config);

        var result = await sut.LoginAsync("EMP001", null, TestHelpers.PlantPlt1Id, false);

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_ReturnsNull_WhenUserNotFound()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var config = CreateConfig();
        var sut = new AuthService(db, config);

        var result = await sut.LoginAsync("NONEXISTENT", "1234", TestHelpers.PlantPlt1Id, false);

        Assert.Null(result);
    }
}
