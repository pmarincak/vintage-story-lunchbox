using Lunchbox.code.block_behaviour;
using Vintagestory.API.Common;

namespace Lunchbox.code.item;

public class ILunchbox : Item
{
    /**
    * \brief Called when the item changes inventory slots.
    */
    public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack)
    {
       if (slot == null || slot.Itemstack == null) return;

        var behaviour = GetCollectibleInterface<CollectableBehaviorLunchbox>();
        var data = behaviour.GetLunchboxData(slot.Itemstack);

        behaviour.ConfigureAutoEat(slot.Itemstack, slot.Inventory, data.GetSlots());
    }

    /**
    * \brief Whether to allow auto-eat or not for items of this type.
    */
    public virtual bool CanAutoEat() { return true; }
}

