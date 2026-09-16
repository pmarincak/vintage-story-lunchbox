using Lunchbox.code.item;

namespace Lunchbox;

public class ItemCooler : ILunchbox
{
    public ItemCooler() : base() 
    {
    }

    public override bool CanAutoEat() { return LunchboxModSystem.config.cooler_autoeat_enabled; }
}