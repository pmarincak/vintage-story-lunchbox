using Vintagestory.API.Common;

namespace Lunchbox;

public class BlockLunchBox : Block, ILunchbox
{
    EntityPlayer? ILunchbox._player_entity { get; set; }

    public BlockLunchBox() : base() { }

    /**
    * \brief Called when the item changes inventory slots.
    */    
    public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack)
    {
        ((ILunchbox)this).ConfigureAutoEat(world, slot.Inventory);
    }

    public ItemSlotBagContent? FindFirstEdibleSlot()
    {
        return ((ILunchbox)this).FindFirstEdibleSlot(GetCollectibleInterface<CollectableBehaviorLunchbox>());
    }
}