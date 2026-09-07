using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Lunchbox;

public class ILunchbox : Item
{
    protected bool auto_eat_enabled = true; //! Whether to allow auto-eat or not for Lunchbox instances. Sub-classes should override.

    // ITEM OVERRIDES

    /**
    * \brief Called when the item changes inventory slots.
    */
    public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack)
    {
       if (!(world is IServerWorldAccessor) || slot == null || slot.Itemstack == null) return;

       ConfigureAutoEat(world, slot.Itemstack, GetLunchboxData(slot.Itemstack));
    }

    // LUNCHBOX CONFIGURATION

    public LunchboxData GetLunchboxData(ItemStack lunchbox)
    {
        var behaviour = GetCollectibleInterface<CollectableBehaviorLunchbox>();
        return behaviour.GetLunchboxData(lunchbox);
    }

    /**
     * \brief Configures auto-eat functionality for this \p lunchbox provided the \p world and the \p inventory the lunchbox resides in.
     * \note Assumes that the inventory contains this lunchbox.
     */
    public void ConfigureAutoEat(IWorldAccessor world, ItemStack lunchbox, LunchboxData data)
    {
        ConfigureAutoEat(world, lunchbox, data, auto_eat_enabled);
    }

    /**
     * \brief Configures auto-eat functionality for this lunchbox provided the \a world and the \a inventory the lunchbox resides in.
     * \note Assumes that the inventory contains this lunchbox.
     * \note If enabled is false then autoeat will not be configured.
     */
    public void ConfigureAutoEat(IWorldAccessor world, ItemStack lunchbox, LunchboxData data, bool enabled)
    {
        if (!enabled)
        {
            return;
        }

        // If the world is not server-side then auto-eat functionality will fail
        if (!(world is IServerWorldAccessor))
        {
            return;
        }

    }

    /**
     * \brief Called when hunger-related statistics are changed. If the current satiety is less than the minimum then auto-eat from the lunchbox inventory.
     */
    public void OnHungerChanged(LunchboxData data)
    {
        var player = data.GetPlayerEntity();
        // Shouldn't happen but just in case
        if (player == null)
        {
            return;
        }

        ITreeAttribute hunger_tree = player.WatchedAttributes.GetTreeAttribute(LunchboxData.HUNGER_KEY);
        var currentsaturation = hunger_tree.GetFloat("currentsaturation");
        if (hunger_tree.GetFloat("currentsaturation") > (float)LunchboxModSystem.config.minimum_satiety)
        {
            return;
        }

        var edible_slot = FindFirstEdibleSlot(data);
        ConsumeItem(edible_slot, data.GetPlayerEntity());

        if (edible_slot != null)
        {
            var currentsaturation_new = hunger_tree.GetFloat("currentsaturation");
            LunchboxModSystem.Log(player, "Saturation change from [" + currentsaturation + "] to [" + currentsaturation_new + "]");
            LunchboxModSystem.Log(player, "Edible slot changed [" + edible_slot.BagIndex + "][" + edible_slot.SlotIndex + "][" + edible_slot.Itemstack?.ToString() + "]");
        }
    }

    /**
     * \brief Called when thirst-related statistics are changed. If the current thirst is less than the minimum then auto-eat from the lunchbox inventory.
     */
    private void OnThirstChanged()
    {
       /* // Shouldn't happen but just in case
        if (_player_entity == null)
        {
            return;
        }

        ITreeAttribute thirst_tree = _player_entity.WatchedAttributes.GetTreeAttribute(THIRST_KEY);
        if (thirst_tree.GetFloat("currentThirst") > (float)LunchboxModSystem.config.minimum_thirst)
        {
            return;
        }

        var drinkable_slot = FindFirstDrinkableSlot();
        ConsumeItem(drinkable_slot);*/
    }

    /**
     * \brief Attempts to consume the food item located in the \a slot.
     */
    private void ConsumeItem(ItemSlotBagContent? slot, EntityPlayer? player)
    {
        const float minimum_seconds = 2; // In order for eating to occur for meals they must have been munched on for at least 2 seconds. The lunchbox fakes this and does it instantly.
        var item = slot?.Itemstack?.Collectible;
        item?.OnHeldInteractStop(minimum_seconds, slot, player, null, null);
    }

    // AUTO EAT SEARCH

    /**
     * \brief Returns the first inventory slot within the lunchbox that contains items with positive satiety values.
     */
    private ItemSlotBagContent? FindFirstEdibleSlot(LunchboxData data)
    {
        return FindFirstValidSlot(data, FoodItemUtility.HasNutritionInformation); 
    }

    /**
     * \brief Returns the first inventory slot within the lunchbox that contains items with positive hydration values.
     */
    private ItemSlotBagContent? FindFirstDrinkableSlot(LunchboxData data) {
        return FindFirstValidSlot(data, FoodItemUtility.HasHydrationInformation);
    }

    /**
     * \brief Retuns the first inventory slot within the lunchbox that contains edible items matching the criteria function.
     */
    private ItemSlotBagContent? FindFirstValidSlot(LunchboxData data, System.Func<ItemSlot?, EntityPlayer?, IWorldAccessor?, bool> CriteriaFunction)
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
        foreach (ItemSlotBagContent? slot in contents)
        {
            if (slot == null) { continue; }

            // Cooked Container Search (ex. Crocks, Pots)
            var item = slot.Itemstack?.Collectible;
            if (cooked_container_slot == null && item is BlockCookedContainerBase)
            {
                var container = item as BlockCookedContainerBase;
                if (container.IsEmpty(slot.Itemstack)) { continue; }
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

