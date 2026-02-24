using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class MockPrivateLlmClientTests
{
    [Fact]
    public async Task InterpretAsync_MapsCurrentScreenFilteredCountIntent()
    {
        var sut = new MockPrivateLlmClient();
        var result = await sut.InterpretAsync("How many records match current filters on this screen?", null, CancellationToken.None);
        Assert.Equal(NlqIntent.CurrentScreenFilteredRecordCount, result.Intent);
    }

    [Fact]
    public async Task InterpretAsync_MapsBottleneckIntent()
    {
        var sut = new MockPrivateLlmClient();
        var result = await sut.InterpretAsync("What is the bottleneck right now?", "day", CancellationToken.None);
        Assert.Equal(NlqIntent.BottleneckWorkCenterNow, result.Intent);
    }

    [Fact]
    public async Task InterpretAsync_MapsDowntimeByAssetIntent()
    {
        var sut = new MockPrivateLlmClient();
        var result = await sut.InterpretAsync("Show downtime by asset this shift", "day", CancellationToken.None);
        Assert.Equal(NlqIntent.DowntimeByAsset, result.Intent);
    }
}
