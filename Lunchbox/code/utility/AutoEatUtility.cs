using Lunchbox.code.inventory;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Lunchbox.code.utility;

/**
 * \brief Contains various utilities used by the Lunchbox for assiting with auto-eat functionality.
 */
public static class AutoEatUtility
{
    /**
    * \brief Called when hunger-related statistics are changed for the \p data. If the current satiety is less than the minimum then auto-eat from the lunchbox inventory.
    */
    public static void OnHungerChanged(LunchboxData data)
    {
        var player = data.GetPlayerEntity();
        // Shouldn't happen but just in case
        if (player == null)
        {
            return;
        }

        ITreeAttribute hunger_tree = player.WatchedAttributes.GetTreeAttribute(LunchboxData.HUNGER_KEY);
        var current_old = hunger_tree.GetFloat("currentsaturation");
        if (current_old > (float)LunchboxModSystem.config.minimum_satiety)
        {
            return;
        }

        var edible_slot = FindFirstEdibleSlot(data);
        if (edible_slot != null)
        {
            ConsumeItem(edible_slot, player);

            var current_new = hunger_tree.GetFloat("currentsaturation");
            LunchboxModSystem.Log(player, "Saturation change from [" + current_old + "] to [" + current_new + "]");
            LunchboxModSystem.Log(player, "Edible slot changed [" + edible_slot.BagIndex + "][" + edible_slot.SlotIndex + "][" + edible_slot.Itemstack?.ToString() + "]");
        }
    }

    /**
     * \brief Called when thirst-related statistics are changed for the \p data. If the current thirst is less than the minimum then auto-eat from the lunchbox inventory.
     */
    public static void OnThirstChanged(LunchboxData data)
    {
        var player = data.GetPlayerEntity();
        // Shouldn't happen but just in case
        if (player == null)
        {
            return;
        }

        ITreeAttribute thirst_tree = player.WatchedAttributes.GetTreeAttribute(LunchboxData.THIRST_KEY);
        if (thirst_tree == null)
        {
            return;
        }

        var current_old = thirst_tree.GetFloat("currentThirst");
        if (current_old > (float)LunchboxModSystem.config.minimum_thirst)
        {
            return;
        }

        var drinkable_slot = FindFirstDrinkableSlot(data);

        if (drinkable_slot != null)
        {
            ConsumeItem(drinkable_slot, player);

            var current_new = thirst_tree.GetFloat("currentThirst");
            LunchboxModSystem.Log(player, "Thirst change from [" + current_old + "] to [" + current_new + "]");
            LunchboxModSystem.Log(player, "Edible slot changed [" + drinkable_slot.BagIndex + "][" + drinkable_slot.SlotIndex + "][" + drinkable_slot.Itemstack?.ToString() + "]");
        }
    }

    /**
     * \brief Attempts to have the \p player consume the food item located in the \p slot.
     */
    private static void ConsumeItem(ItemSlotBagContent? slot, EntityPlayer? player)
    {
        const float minimum_seconds = 2; // In order for eating to occur for meals they must have been munched on for at least 2 seconds. The lunchbox fakes this and does it instantly.
        var item = slot?.Itemstack?.Collectible;
        item?.OnHeldInteractStop(minimum_seconds, slot, player, null, null);
    }

    /**
     * \brief Returns the first inventory slot within the lunchbox \p data that contains items with positive satiety values.
     */
    private static ItemSlotBagContent? FindFirstEdibleSlot(LunchboxData data)
    {
        return FindFirstValidSlot(data, FoodItemUtility.HasNutritionInformation);
    }

    /**
     * \brief Returns the first inventory slot within the lunchbox \p data that contains items with positive hydration values.
     */
    private static ItemSlotBagContent? FindFirstDrinkableSlot(LunchboxData data)
    {
        return FindFirstValidSlot(data, FoodItemUtility.HasHydrationInformation);
    }

    /**
     * \brief Retuns the first inventory slot within the lunchbox \p data that contains edible items matching the \p CriteriaFunction.
     */
    private static ItemSlotBagContent? FindFirstValidSlot(LunchboxData data, System.Func<ItemSlot?, EntityPlayer?, IWorldAccessor?, bool> CriteriaFunction)
    {
        var contents = data.GetSlots();

        if (contents == null)
        {
            return null;
        }

        var player = data.GetPlayerEntity();

        ItemSlotBagContent? cooked_container_slot = null;
        ItemSlotBagContent? meal_holding_container_slot = null;
        ItemSlotBagContent? first_edible_slot = null;
        
        var world = player?.World;
        if (world == null)
        {
            return null;
        }

        foreach (ItemSlotBagContent? slot in contents)
        {
            if (slot == null) { continue; }

            // Cooked Container Search (ex. Crocks, Pots)
            var item = slot.Itemstack?.Collectible;
            if (cooked_container_slot == null && item is BlockCookedContainerBase)
            {
                var container = item as BlockCookedContainerBase;
                if (container == null || container.IsEmpty(slot.Itemstack)) { continue; }
                if (!CriteriaFunction(slot, player, world)) { continue; }

                cooked_container_slot = slot;
            }

            // Meal Holding Container Check (ex. Bowls)
            if (meal_holding_container_slot == null && FoodItemUtility.IsMealHoldingContainer(slot))
            {
                if (CriteriaFunction(slot, player, world)) { continue; }

                meal_holding_container_slot = slot;
            }

            // If our slot has nutrition information
            // This could also include cooked containers so let's make sure we don't select it
            if (first_edible_slot == null && item is not BlockCookedContainerBase && CriteriaFunction(slot, player, world))
            {
                first_edible_slot = slot;
            }

            // We found an edible slot and we haven't found a cooked container so this is the slot we want to eat
            if (first_edible_slot != null && cooked_container_slot == null) { return first_edible_slot; }

            // We haven't found both a container or a meal container so keep looking
            if (meal_holding_container_slot == null || cooked_container_slot == null) { continue; }

            // Make Meal
            var cooked_container = cooked_container_slot.Itemstack?.Collectible as BlockCookedContainerBase;
            bool? result = cooked_container?.ServeIntoStack(meal_holding_container_slot, cooked_container_slot, world);
            if (result != true)
            {
                meal_holding_container_slot = null;
                continue;
            }

            return meal_holding_container_slot;
        }

        return first_edible_slot;
    }
}