using ArchipelagoP5RMod;
using Xunit;

namespace ArchipelagoP5RMod.Tests;

public class ConquestManagerTests
{
    [Theory]
    [InlineData(21, Palaces.KAMOSHIDA)]
    [InlineData(50, Palaces.MADARAME)]
    [InlineData(90, Palaces.KANESHIRO)]
    [InlineData(120, Palaces.FUTABA)]
    [InlineData(180, Palaces.OKUMURA)]
    [InlineData(220, Palaces.SAE)]
    [InlineData(250, Palaces.SHIDO)]
    [InlineData(267, Palaces.MEMENTOS_DEPTHS)]
    [InlineData(290, Palaces.MARUKI)]
    [InlineData(5, Palaces.NONE)]
    public void TotalDaysToPalace_MapsCorrectly(short totalDays, Palaces expectedPalace)
    {
        Palaces actual = ConquestManager.TotalDaysToPalace(totalDays);
        Assert.Equal(expectedPalace, actual);
    }
}
