using Vintagestory.API.Common;

namespace Lunchbox;
/**
 * \brief An ItemSlot for bags which will only hold items with nutritional information or meal containers.
 * \note Because there is no clear way to determine what is "food" this ItemSlot does as rough estimation based on vanilla properties. 
 * Should theoretically work with other mods assuming those vanilla properties are set correctly.
 */
class FoodSlot : ItemSlotBagContent
{
    public FoodSlot(InventoryBase inventory, int BagIndex, int SlotIndex, EnumItemStorageFlags storageType) : base(inventory, BagIndex, SlotIndex, storageType)
    {
    }

    public override bool CanTakeFrom(ItemSlot source_slot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
    {
        return CanStoreItem(source_slot) && base.CanTakeFrom(source_slot, priority);
    }

    public override bool CanHold(ItemSlot source_slot)
    {
        return CanStoreItem(source_slot) && base.CanHold(source_slot);
    }

    /**
     * \brief Returns whether we can store the items held by \a source_slot.
     */
    private bool CanStoreItem(ItemSlot source_slot)
    {
        // We need to use our inventory not the inventory of the source_slot as the source_slot may be coming from an incompatible inventory
        var player_entity = FoodItemUtility.GetPlayerOwnerFromInventory(inventory);
        var world = player_entity == null ? inventory.Api.World : player_entity.World;
        bool food_item_has_satiety = FoodItemUtility.HasNutritionInformation(source_slot, player_entity, world);
        bool is_meal_container = FoodItemUtility.IsMealContainer(source_slot);

        return food_item_has_satiety || is_meal_container;
    }
}