using Lunchbox.code.item;

namespace Lunchbox;

public class ItemTemporalBackpack : ILunchbox
{
    public ItemTemporalBackpack() : base()
    {
        auto_eat_enabled = LunchboxModSystem.config.temporal_backpack_autoeat_enabled;
    }
}