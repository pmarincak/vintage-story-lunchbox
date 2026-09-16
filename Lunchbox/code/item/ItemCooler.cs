using Lunchbox.code.item;

namespace Lunchbox;

public class ItemCooler : ILunchbox
{
    public ItemCooler() : base() 
    {
        auto_eat_enabled = LunchboxModSystem.config.cooler_autoeat_enabled;
    }
}