using Vintagestory.API.Common;

namespace Lunchbox;

public class ItemTemporalBackpack : ItemLunchBox
{
    public ItemTemporalBackpack() : base()
    {
        auto_eat_enabled = LunchboxModSystem.config.temporal_backpack_autoeat_enabled;
    }
}