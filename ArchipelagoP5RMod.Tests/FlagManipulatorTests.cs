using ArchipelagoP5RMod;
using Xunit;

namespace ArchipelagoP5RMod.Tests;

public class FlagManipulatorTests
{
    [Fact]
    public void ExternalBitSection_Constants_AreValid()
    {
        Assert.Equal(0x60000001u, FlagManipulator.SHOWING_MESSAGE);
        Assert.Equal(0x60000002u, FlagManipulator.SHOWING_GAME_MSG);
        Assert.Equal(0x60000003u, FlagManipulator.OVERWRITE_ITEM_TEXT);
    }

    [Fact]
    public void ExternalCountSection_Constants_AreValid()
    {
        Assert.Equal(0x10000000u, FlagManipulator.AP_LAST_REWARD_INDEX);
        Assert.Equal(0x10000001u, FlagManipulator.AP_CURR_REWARD_CMM_ABILITY);
        Assert.Equal(0x10000002u, FlagManipulator.AP_CURR_REWARD_ITEM_ID);
        Assert.Equal(0x10000003u, FlagManipulator.AP_CURR_REWARD_ITEM_NUM);
        Assert.Equal(0x10000004u, FlagManipulator.AP_CURR_NOTIFY_PALACE);
    }
}
