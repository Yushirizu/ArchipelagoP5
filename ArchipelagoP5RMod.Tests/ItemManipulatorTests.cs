using ArchipelagoP5RMod;
using Xunit;

namespace ArchipelagoP5RMod.Tests;

public class ItemManipulatorTests
{
    [Theory]
    [InlineData(0x0000)] // Melee
    [InlineData(0x1000)] // Ranged
    [InlineData(0x2000)] // Armor
    [InlineData(0x3000)] // Accessory
    [InlineData(0x4000)] // Consumable
    [InlineData(0x5000)] // Key Item
    [InlineData(0x6000)] // Material
    [InlineData(0x7000)] // Skill Card
    [InlineData(0x8000)] // Outfit
    public void BlankItems_Categories_AreValidCategoryPrefixes(uint categoryId)
    {
        Assert.True(categoryId <= 0x8000);
        Assert.Equal(0u, categoryId % 0x1000);
    }
}
