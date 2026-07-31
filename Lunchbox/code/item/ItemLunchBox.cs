using Vintagestory.API.Common;

namespace Lunchbox;

public class ItemLunchBox : Item, ILunchbox
{
    EntityPlayer? ILunchbox._player_entity { get; set; }

    public ItemLunchBox() : base() { }

    public ItemLunchBox(int itemId) : base(itemId) { }

    /**
    * \brief Called when the item changes inventory slots.
    */    
    public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack)
    {
        ((ILunchbox)this).ConfigureAutoEat(world, slot.Inventory);
    }

    public ItemSlotBagContent? FindFirstEdibleSlot()
    {
        return ((ILunchbox)this).FindFirstValidSlot(GetCollectibleInterface<CollectableBehaviorLunchbox>(), FoodItemUtility.HasNutritionInformation);
    }

    public ItemSlotBagContent? FindFirstDrinkableSlot()
    {
        return ((ILunchbox)this).FindFirstValidSlot(GetCollectibleInterface<CollectableBehaviorLunchbox>(), FoodItemUtility.HasHydrationInformation);
    }
}