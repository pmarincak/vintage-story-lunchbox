using Lunchbox.code.item;

namespace Lunchbox;

public class ItemTemporalBackpack : ILunchbox
{
    public ItemTemporalBackpack() : base()
    {
    }

    public override bool CanAutoEat() { return LunchboxModSystem.config.temporal_backpack_autoeat_enabled;}
}