using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Lunchbox;
internal interface ILunchbox
{
    static private string HUNGER_KEY = "hunger"; //! Key for hunger-related statistics for the player
    static private float MIN_SATIETY = 15.0f; //! Minimum satiety that the player has before the lunchbox auto-eats
    EntityPlayer? _player_entity { get; set; } //! Player entity that has the lunchbox equiped. If null then the box is not equipped.

    /**
     * \brief Configures auto-eat functionality for this lunchbox provided the \a world and the \a inventory the lunchbox resides in.
     * \note Assumes that the inventory contains this lunchbox.
     */
    public void ConfigureAutoEat(IWorldAccessor world, InventoryBase inventory)
    {
        // If the world is not server-side then auto-eat functionality will fail
        if (!(world is IServerWorldAccessor))
        {
            return;
        }

        var next_player_entity = FoodItemUtility.GetPlayerFromInventory(inventory);

        // No Change Needed
        if (_player_entity == next_player_entity)
        {
            return;
        }

        _player_entity?.WatchedAttributes.UnregisterListener(OnHungerChanged);
        _player_entity = next_player_entity;
        _player_entity?.WatchedAttributes.RegisterModifiedListener(HUNGER_KEY, OnHungerChanged);
    }

    /**
     * \brief Called when hunger-related statistics are changed. If the current satiety is less than the minimum then auto-eat from the lunchbox inventory.
     */
    private void OnHungerChanged()
    {
        // Shouldn't happen but just in case
        if (_player_entity == null)
        {
            return;
        }

        ITreeAttribute hunger_tree = _player_entity.WatchedAttributes.GetTreeAttribute(HUNGER_KEY);
        if (hunger_tree.GetFloat("currentsaturation") > MIN_SATIETY)
        {
            return;
        }

        var edible_slot = FindFirstEdibleSlot();
        EatFood(edible_slot);
    }

    /**
     * \brief Attempts to eat the food located in the \a slot.
     * \note Assumes that the item in the \a slot is food.
     */
    private void EatFood(ItemSlotBagContent? slot)
    {
        const float minimum_seconds = 2; // In order for eating to occur for meals they must have been munched on for at least 2 seconds. The lunchbox fakes this and does it instantly.
        var item = slot?.Itemstack?.Collectible;
        item?.OnHeldInteractStop(minimum_seconds, slot, _player_entity, null, null);
    }

    /**
     * \brief Retuns the first inventory slot within the lunchbox that contains edible items.
     */
    abstract ItemSlotBagContent? FindFirstEdibleSlot();

    /**
     * \brief Retuns the first inventory slot within the lunchbox that contains edible items.
     */
    public ItemSlotBagContent? FindFirstEdibleSlot(CollectableBehaviorLunchbox collectibleInterface)
    {
        if (collectibleInterface == null)
        {
            return null;
        }

        var contents = collectibleInterface._slots;

        ItemSlotBagContent? cooked_container_slot = null;
        ItemSlotBagContent? meal_holding_container_slot = null;
        ItemSlotBagContent? first_edible_slot = null;
        foreach (ItemSlotBagContent? slot in contents)
        {
            if (slot == null) { continue; }

            // Cooked Container Search (ex. Crocks, Pots)
            var item = slot.Itemstack?.Collectible;
            if (cooked_container_slot == null && item is BlockCookedContainerBase)
            {
                var container = item as BlockCookedContainerBase;
                if (container.IsEmpty(slot.Itemstack)) { continue; }
                if (!FoodItemUtility.HasNutritionInformation(slot, _player_entity)) { continue; }

                cooked_container_slot = slot;
            }

            // Meal Holding Container Check (ex. Bowls)
            if (meal_holding_container_slot == null && FoodItemUtility.IsMealHoldingContainer(slot))
            {
                if (FoodItemUtility.HasNutritionInformation(slot, _player_entity)) { continue; }

                meal_holding_container_slot = slot;
            }

            // If our slot has nutrition information
            // This could also include cooked containers so let's make sure we don't select it
            if (first_edible_slot == null && item is not BlockCookedContainerBase && FoodItemUtility.HasNutritionInformation(slot, _player_entity))
            {
                first_edible_slot = slot;
            }

            // We found an edible slot and we haven't found a cooked container so this is the slot we want to eat
            if (first_edible_slot != null && cooked_container_slot == null) { return first_edible_slot; }

            // We haven't found both a container or a meal container so keep looking
            if (meal_holding_container_slot == null || cooked_container_slot == null) { continue; }

            // Make Meal
            var cooked_container = cooked_container_slot.Itemstack?.Collectible as BlockCookedContainerBase;
            bool? result = cooked_container?.ServeIntoStack(meal_holding_container_slot, cooked_container_slot, _player_entity.World);
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

