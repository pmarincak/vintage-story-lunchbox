using Vintagestory.API.Common;

namespace Lunchbox;

public class ItemCooler : ItemLunchBox
{
    public override void ConfigureAutoEat(IWorldAccessor world, InventoryBase inventory)
    {
        ((ILunchbox)this).ConfigureAutoEat(world, inventory, LunchboxModSystem.config.cooler_autoeat_enabled);
    }
}