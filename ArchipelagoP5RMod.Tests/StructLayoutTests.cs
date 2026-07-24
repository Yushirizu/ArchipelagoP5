using System.Runtime.InteropServices;
using ArchipelagoP5RMod.Types;
using Xunit;

namespace ArchipelagoP5RMod.Tests;

public class StructLayoutTests
{
    [Fact]
    public void FlowCommandData_FieldOffsets_MatchNativeP5RLayout()
    {
        Assert.Equal(0x0, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.CurrFuncName)));
        Assert.Equal(0x28, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.CurrInstructionIndex)));
        Assert.Equal(0x2C, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.StackSize)));
        Assert.Equal(0x30, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.ArgTypes)));
        Assert.Equal(0x5F, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.ReturnType)));
        Assert.Equal(0x60, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.ArgData)));
        Assert.Equal(0x1D8, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.ReturnValue)));
        Assert.Equal(0x1E0, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.FileHeader)));
        Assert.Equal(0x1E8, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.FileLabels)));
        Assert.Equal(0x1F0, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.ProcedureEntries)));
        Assert.Equal(0x1F8, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.LabelEntries)));
        Assert.Equal(0x200, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.InstructionData)));
        Assert.Equal(0x208, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.MessageScriptData)));
        Assert.Equal(0x210, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.StringData)));
        Assert.Equal(0x218, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.CurrFuncIndex)));
        Assert.Equal(0x21C, (int)Marshal.OffsetOf<FlowCommandData>(nameof(FlowCommandData.someIndex)));
    }

    [Fact]
    public void DateInfo_FieldOffsets_MatchNativeP5RLayout()
    {
        Assert.Equal(0x0, (int)Marshal.OffsetOf<DateInfo>(nameof(DateInfo.currTotalDays)));
        Assert.Equal(0x2, (int)Marshal.OffsetOf<DateInfo>(nameof(DateInfo.currTime)));
        Assert.Equal(0x3, (int)Marshal.OffsetOf<DateInfo>(nameof(DateInfo.unknown_flag)));
        Assert.Equal(0x4, (int)Marshal.OffsetOf<DateInfo>(nameof(DateInfo.nextTotalDays)));
        Assert.Equal(0x6, (int)Marshal.OffsetOf<DateInfo>(nameof(DateInfo.nextTime)));
    }

    [Fact]
    public void FlowReturnType_EnumValues_MatchNativeVM()
    {
        Assert.Equal((byte)0, (byte)FlowReturnType.Int);
        Assert.Equal((byte)1, (byte)FlowReturnType.Float);
    }
}
