using ArchipelagoP5RMod;
using ArchipelagoP5RMod.Types;
using Xunit;

namespace ArchipelagoP5RMod.Tests;

public class ScheduleTransitionTests
{
    [Theory]
    [InlineData(4, 5, TypeOfDay.None)]
    [InlineData(4, 7, TypeOfDay.Setup)]
    [InlineData(4, 22, TypeOfDay.LoopDay)]
    [InlineData(4, 23, TypeOfDay.InfiltrationDay)]
    [InlineData(5, 23, TypeOfDay.LoopDay)]
    [InlineData(5, 24, TypeOfDay.InfiltrationDay)]
    public void ToTypeOfDay_MapsAllCalendarKeypoints(uint month, uint day, TypeOfDay expectedType)
    {
        TypeOfDay actual = DateManipulator.ToTypeOfDay(month, day);
        Assert.Equal(expectedType, actual);
    }

    [Fact]
    public void SetupTotalDay_IsDaySix()
    {
        Assert.Equal(6, DateManipulator.SETUP_TOTAL_DAY);
    }

    [Theory]
    [InlineData(4, 1, 0)]
    [InlineData(4, 7, 6)]
    [InlineData(4, 22, 21)]
    [InlineData(5, 23, 52)]
    public void GetTotalDays_CalculatesExpectedTotalDays(uint month, uint day, int expectedTotalDays)
    {
        int totalDays = DateManipulator.GetTotalDays(month, day);
        Assert.Equal(expectedTotalDays, totalDays);
    }

    [Fact]
    public void SetupDay_TransitionsTo_FirstLoopDay21()
    {
        short setupTotalDay = DateManipulator.SETUP_TOTAL_DAY; // 6 (April 7)
        short firstLoopTotalDay = 21; // April 22

        Assert.Equal(6, setupTotalDay);
        Assert.Equal(TypeOfDay.Setup, DateManipulator.ToTypeOfDay(4, 7));
        Assert.Equal(TypeOfDay.LoopDay, DateManipulator.ToTypeOfDay(4, 22));
        Assert.True(firstLoopTotalDay > setupTotalDay, "First loop day must be strictly after setup day to prevent infinite loop");
    }

    [Fact]
    public void SetupTime_IsFour()
    {
        Assert.Equal(4, ScheduleManipulator.SETUP_TIME);
    }

    [Fact]
    public void WarpToLeblanc_IndexIsEight()
    {
        Assert.Equal(8, (int)CustomApMethodsIndexes.WarpToLeblanc);
    }
}
