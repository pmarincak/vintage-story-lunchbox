using Vintagestory.API.Common;

namespace Lunchbox;

public class ItemTemporalBackpack : ItemLunchBox
{
    public override void ConfigureAutoEat(IWorldAccessor world, InventoryBase inventory)
    {
        ((ILunchbox)this).ConfigureAutoEat(world, inventory, LunchboxModSystem.config.temporal_backpack_autoeat_enabled);
    }
}