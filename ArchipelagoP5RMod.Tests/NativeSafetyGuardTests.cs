using ArchipelagoP5RMod;
using Xunit;

namespace ArchipelagoP5RMod.Tests;

public class NativeSafetyGuardTests
{
    [Fact]
    public void IsValidPointer_NullAndLowAddresses_ReturnFalse()
    {
        Assert.False(NativeSafetyGuard.IsValidPointer(IntPtr.Zero));
        Assert.False(NativeSafetyGuard.IsValidPointer((IntPtr)0x10));
        Assert.False(NativeSafetyGuard.IsValidPointer((IntPtr)0x4000));
        Assert.False(NativeSafetyGuard.IsValidPointer((IntPtr)0xFFFF));
    }

    [Fact]
    public void IsValidPointer_ValidUserSpaceAddresses_ReturnTrue()
    {
        Assert.True(NativeSafetyGuard.IsValidPointer((IntPtr)0x10000));
        Assert.True(NativeSafetyGuard.IsValidPointer(unchecked((IntPtr)0x140000000L)));
        Assert.True(NativeSafetyGuard.IsValidPointer(unchecked((IntPtr)0x7FFFFFFFFFFFL)));
    }

    [Fact]
    public void IsValidPointer_KernelMemorySpaceAddresses_ReturnFalse()
    {
        Assert.False(NativeSafetyGuard.IsValidPointer(unchecked((IntPtr)0x8000000000000000L)));
        Assert.False(NativeSafetyGuard.IsValidPointer(new IntPtr(-1))); // 0xFFFFFFFFFFFFFFFF
    }

    [Fact]
    public void ExecuteSafe_CatchesExceptionAndReturnsFalse()
    {
        bool executed = false;
        bool result = NativeSafetyGuard.ExecuteSafe(() =>
        {
            executed = true;
            throw new InvalidOperationException("Test exception");
        }, "UnitTest");

        Assert.True(executed);
        Assert.False(result);
    }
}
