using Vintagestory.API.Common;

namespace Lunchbox;

public class ILunchbox : Item
{
    protected bool auto_eat_enabled = true; //! Whether to allow auto-eat or not for Lunchbox instances. Sub-classes should override.

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
}

