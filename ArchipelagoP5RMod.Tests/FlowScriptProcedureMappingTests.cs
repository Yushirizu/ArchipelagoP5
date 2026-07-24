using System;
using System.IO;
using ArchipelagoP5RMod;
using Xunit;

namespace ArchipelagoP5RMod.Tests;

public class FlowScriptProcedureMappingTests
{
    [Fact]
    public void CustomApMethodsIndexes_EnumValues_AreSequentialAndZeroBased()
    {
        var values = (CustomApMethodsIndexes[])Enum.GetValues(typeof(CustomApMethodsIndexes));
        Assert.Equal(9, values.Length);

        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(i, (int)values[i]);
        }
    }

    [Theory]
    [InlineData(CustomApMethodsIndexes.RewardItemsFunc, 0)]
    [InlineData(CustomApMethodsIndexes.NotifyConfidantAbilityReward, 1)]
    [InlineData(CustomApMethodsIndexes.NotifyConfidantLocation, 2)]
    [InlineData(CustomApMethodsIndexes.NotifyInfiltrationRoute, 3)]
    [InlineData(CustomApMethodsIndexes.Test, 4)]
    [InlineData(CustomApMethodsIndexes.NotifyMissingSaveDirectoryError, 5)]
    [InlineData(CustomApMethodsIndexes.NewGameSetupSdl, 6)]
    [InlineData(CustomApMethodsIndexes.NotifyPartyMemberJoined, 7)]
    [InlineData(CustomApMethodsIndexes.WarpToLeblanc, 8)]
    public void CustomApMethodsIndexes_ExpectedExplicitIndexes(CustomApMethodsIndexes index, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)index);
    }

    [Fact]
    public void ApMethodsFlowFile_ContainsExpectedProcedureDeclarations()
    {
        string flowPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "ArchipelagoP5RMod", "FlowFiles", "src", "AP_Methods.flow");
        string fullPath = Path.GetFullPath(flowPath);

        Assert.True(File.Exists(fullPath), $"AP_Methods.flow not found at {fullPath}");

        string content = File.ReadAllText(fullPath);
        Assert.Contains("void RewardItems()", content);
        Assert.Contains("void NotifyConfidantAbilityReward()", content);
        Assert.Contains("void NotifyConfidantLocation()", content);
        Assert.Contains("void NotifyInfiltration()", content);
        Assert.Contains("void Test()", content);
        Assert.Contains("void NotifyMissingSaveDirectory()", content);
        Assert.Contains("void NewGameSetupSdl()", content);
        Assert.Contains("void NotifyPartyMemberJoined()", content);
        Assert.Contains("void WarpToLeblanc()", content);
    }
}
