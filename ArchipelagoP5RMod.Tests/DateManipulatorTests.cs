using ArchipelagoP5RMod;
using ArchipelagoP5RMod.Types;
using Xunit;

namespace ArchipelagoP5RMod.Tests;

public class DateManipulatorTests
{
    [Fact]
    public void GetTotalDays_AprilFirst_ReturnsZero()
    {
        int days = DateManipulator.GetTotalDays(4, 1);
        Assert.Equal(0, days);
    }

    [Fact]
    public void GetTotalDays_AprilSeventh_ReturnsSetupTotalDay()
    {
        int days = DateManipulator.GetTotalDays(4, 7);
        Assert.Equal(DateManipulator.SETUP_TOTAL_DAY, days);
    }

    [Fact]
    public void GetTotalDays_April22nd_ReturnsLoopDay21()
    {
        int days = DateManipulator.GetTotalDays(4, 22);
        Assert.Equal(21, days);
    }

    [Fact]
    public void ToTypeOfDay_SetupDay_ReturnsSetup()
    {
        TypeOfDay type = DateManipulator.ToTypeOfDay(4, 7);
        Assert.Equal(TypeOfDay.Setup, type);
    }

    [Fact]
    public void ToTypeOfDay_LoopDay21_ReturnsLoopDay()
    {
        TypeOfDay type = DateManipulator.ToTypeOfDay(4, 22);
        Assert.Equal(TypeOfDay.LoopDay, type);
    }

    [Fact]
    public void ToTypeOfDay_DayAfterLoopDay_ReturnsInfiltrationDay()
    {
        TypeOfDay type = DateManipulator.ToTypeOfDay(4, 23);
        Assert.Equal(TypeOfDay.InfiltrationDay, type);
    }

    [Fact]
    public void ToTypeOfDay_UnmappedDay_ReturnsNone()
    {
        TypeOfDay type = DateManipulator.ToTypeOfDay(4, 15);
        Assert.Equal(TypeOfDay.None, type);
    }

    [Fact]
    public void GetMonthFromTotalDays_ReturnsCorrectMonth()
    {
        Month month = DateManipulator.GetMonthFromTotalDays(6);
        Assert.Equal(Month.April, month);
    }
}
