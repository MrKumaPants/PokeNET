using PokeNET.Core.Data;
using Xunit;

namespace PokeNET.Core.Tests.Data;

/// <summary>
/// Unit tests for ItemData model.
/// Tests item data loading, consumable/held flags, and categorization.
/// </summary>
public class ItemDataTests
{
    [Fact]
    public void ItemData_Medicine_IsConsumable()
    {
        // Arrange & Act
        var potion = new ItemData
        {
            Id = "potion",
            Name = "Potion",
            Category = ItemCategory.Medicine,
            BuyPrice = 200,
            SellPrice = 100,
            Consumable = true,
            UsableInBattle = true,
            UsableOutsideBattle = true,
        };

        // Assert
        Assert.Equal(ItemCategory.Medicine, potion.Category);
        Assert.True(potion.Consumable);
        Assert.True(potion.UsableInBattle);
        Assert.True(potion.UsableOutsideBattle);
    }

    [Fact]
    public void ItemData_Pokeball_IsConsumable()
    {
        // Arrange & Act
        var pokeball = new ItemData
        {
            Id = "pokeball",
            Name = "Pokeball",
            Category = ItemCategory.Pokeball,
            BuyPrice = 200,
            SellPrice = 100,
            Consumable = true,
            UsableInBattle = true,
            UsableOutsideBattle = false,
        };

        // Assert
        Assert.Equal(ItemCategory.Pokeball, pokeball.Category);
        Assert.True(pokeball.Consumable);
        Assert.True(pokeball.UsableInBattle);
        Assert.False(pokeball.UsableOutsideBattle);
    }

    [Fact]
    public void ItemData_HeldItem_IsNotConsumable()
    {
        // Arrange & Act
        var leftovers = new ItemData
        {
            Id = "leftovers",
            Name = "Leftovers",
            Category = ItemCategory.HeldItem,
            BuyPrice = 0, // Cannot buy
            SellPrice = 100,
            Consumable = false,
            Holdable = true,
        };

        // Assert
        Assert.Equal(ItemCategory.HeldItem, leftovers.Category);
        Assert.False(leftovers.Consumable);
        Assert.True(leftovers.Holdable);
        Assert.Equal(0, leftovers.BuyPrice);
    }

    [Fact]
    public void ItemData_KeyItem_CannotBeSold()
    {
        // Arrange & Act
        var keyItem = new ItemData
        {
            Id = "bicycle",
            Name = "Bicycle",
            Category = ItemCategory.KeyItem,
            BuyPrice = 0,
            SellPrice = 0,
            Consumable = false,
        };

        // Assert
        Assert.Equal(ItemCategory.KeyItem, keyItem.Category);
        Assert.Equal(0, keyItem.BuyPrice);
        Assert.Equal(0, keyItem.SellPrice);
    }

    [Fact]
    public void ItemData_TM_Properties()
    {
        // Arrange & Act
        var tm = new ItemData
        {
            Id = "tm01",
            Name = "TM01",
            Category = ItemCategory.TM,
            BuyPrice = 3000,
            SellPrice = 1500,
            Consumable = true,
            UsableOutsideBattle = true,
        };

        // Assert
        Assert.Equal(ItemCategory.TM, tm.Category);
        Assert.True(tm.Consumable);
        Assert.True(tm.UsableOutsideBattle);
    }

    [Fact]
    public void ItemData_Berry_IsConsumableAndHoldable()
    {
        // Arrange & Act
        var berry = new ItemData
        {
            Id = "oran-berry",
            Name = "Oran Berry",
            Category = ItemCategory.Berry,
            BuyPrice = 0,
            SellPrice = 10,
            Consumable = true,
            Holdable = true,
            UsableInBattle = true,
        };

        // Assert
        Assert.Equal(ItemCategory.Berry, berry.Category);
        Assert.True(berry.Consumable);
        Assert.True(berry.Holdable);
    }

    [Fact]
    public void ItemData_EvolutionStone_Properties()
    {
        // Arrange & Act
        var stone = new ItemData
        {
            Id = "fire-stone",
            Name = "Fire Stone",
            Category = ItemCategory.EvolutionItem,
            BuyPrice = 2100,
            SellPrice = 1050,
            Consumable = true,
            UsableOutsideBattle = true,
        };

        // Assert
        Assert.Equal(ItemCategory.EvolutionItem, stone.Category);
        Assert.True(stone.Consumable);
    }

    [Fact]
    public void ItemData_SellPrice_TypicallyHalfOfBuyPrice()
    {
        // Arrange & Act
        var item = new ItemData
        {
            Name = "Super Potion",
            BuyPrice = 700,
            SellPrice = 350,
        };

        // Assert
        Assert.Equal(item.BuyPrice / 2, item.SellPrice);
    }

    [Fact]
    public void ItemData_EffectScript_IsOptional()
    {
        // Arrange & Act
        var itemWithScript = new ItemData
        {
            Name = "Potion",
            EffectScript = "scripts/items/potion.csx",
            EffectParameters = new() { { "healAmount", 20 } },
        };

        var itemWithoutScript = new ItemData { Name = "Bicycle" };

        // Assert
        Assert.NotNull(itemWithScript.EffectScript);
        Assert.NotNull(itemWithScript.EffectParameters);
        Assert.Null(itemWithoutScript.EffectScript);
        Assert.Null(itemWithoutScript.EffectParameters);
    }

    [Fact]
    public void ItemData_SpritePath_IsOptional()
    {
        // Arrange & Act
        var itemWithSprite = new ItemData
        {
            Name = "Potion",
            SpritePath = "sprites/items/potion.png",
        };

        var itemWithoutSprite = new ItemData { Name = "New Item" };

        // Assert
        Assert.Equal("sprites/items/potion.png", itemWithSprite.SpritePath);
        Assert.Null(itemWithoutSprite.SpritePath);
    }

    [Fact]
    public void ItemData_Description_CanBeEmpty()
    {
        // Arrange & Act
        var item = new ItemData { Name = "Test Item" };

        // Assert
        Assert.NotNull(item.Description);
        Assert.Empty(item.Description);
    }

    [Fact]
    public void ItemData_BattleItem_UsableInBattle()
    {
        // Arrange & Act
        var xAttack = new ItemData
        {
            Name = "X Attack",
            Category = ItemCategory.BattleItem,
            Consumable = true,
            UsableInBattle = true,
            UsableOutsideBattle = false,
        };

        // Assert
        Assert.Equal(ItemCategory.BattleItem, xAttack.Category);
        Assert.True(xAttack.UsableInBattle);
        Assert.False(xAttack.UsableOutsideBattle);
    }

    [Theory]
    [InlineData(ItemCategory.Medicine)]
    [InlineData(ItemCategory.Pokeball)]
    [InlineData(ItemCategory.BattleItem)]
    [InlineData(ItemCategory.HeldItem)]
    [InlineData(ItemCategory.EvolutionItem)]
    [InlineData(ItemCategory.TM)]
    [InlineData(ItemCategory.KeyItem)]
    [InlineData(ItemCategory.Berry)]
    [InlineData(ItemCategory.Miscellaneous)]
    public void ItemData_AllCategories_AreValid(ItemCategory category)
    {
        // Arrange & Act
        var item = new ItemData { Name = "Test Item", Category = category };

        // Assert
        Assert.Equal(category, item.Category);
    }

    [Fact]
    public void ItemData_DefaultConsumable_IsTrue()
    {
        // Arrange & Act
        var item = new ItemData();

        // Assert
        Assert.True(item.Consumable);
    }
}
