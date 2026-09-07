using Vintagestory.API.Common;

namespace Lunchbox;

public class ItemCooler : ItemLunchBox
{
    public ItemCooler() : base() {
        auto_eat_enabled = LunchboxModSystem.config.cooler_autoeat_enabled;
    }
}