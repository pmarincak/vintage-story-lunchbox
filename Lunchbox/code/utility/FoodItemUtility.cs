using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Lunchbox;

/**
 * \brief Contains various utilities used by the Lunchbox to determine food items & related functionality.
 */
public static class FoodItemUtility
{
    /**
     * \brief Returns whether the \a slot in the \a player_entity's inventory contains nutritional information or not.
     */
    public static bool HasNutritionInformation(ItemSlot? slot, EntityPlayer? player_entity, IWorldAccessor? world)
    {
        var item = slot?.Itemstack?.Collectible;
        FoodNutritionProperties? nutrition = item?.GetNutritionProperties(world, slot?.Itemstack, player_entity);

        if (nutrition?.Satiety > 0.0f)
        {
            return true;
        }

        BlockMeal? block_meal = item as BlockMeal;
        FoodNutritionProperties[]? nutrition_props = block_meal?.GetContentNutritionProperties(world, slot, player_entity);
        if (nutrition_props != null && nutrition_props.Length > 0)
        {
            bool result = true;
            foreach (FoodNutritionProperties? props in nutrition_props)
            {
                result = result && props?.Satiety > 0.0f;
                if (!result) { break; }
            }

            return result;
        }

        BlockCookedContainerBase? block_container = item as BlockCookedContainerBase;
        if (block_container != null && world != null)
        {
            var contents = block_container.GetNonEmptyContents(world, slot?.Itemstack);
            return contents.Length > 0 && !MealMeshCache.ContentsRotten(contents);
        }

        return false;
    }

    /**
     * \brief Returns whether the item held in \a slot is a meal container or not.
     * \note A meal container is a bowl, a pot, a crock, etc.
     */
    public static bool IsMealContainer(ItemSlot? slot)
    {
        return slot?.Itemstack?.Collectible?.Attributes?["mealContainer"].AsBool() == true;
    }

    /**
     * \brief Returns the PlayerEntity which owns the \a inventory, if applicable.
     */
    public static EntityPlayer? GetPlayerOwnerFromInventory(InventoryBase inventory)
    {
        InventoryBasePlayer? backpack_inventory = inventory as InventoryBasePlayer;
        return backpack_inventory?.Player.Entity;
    }

    /**
     * \brief Returns whether the item in \a slot is a meal container that can hold meals or not.
     * \note There is no way to be sure what a "meal holding container" is (ex. Bowl). For now we check the code to verify it is a bowl.
     */
    public static bool IsMealHoldingContainer(ItemSlot? slot)
    {
        return IsMealContainer(slot) && slot?.Itemstack?.Collectible?.FirstCodePart().ToString() == "bowl";
    }
}



