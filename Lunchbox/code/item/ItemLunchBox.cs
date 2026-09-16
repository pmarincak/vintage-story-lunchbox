using Lunchbox.code.item;

namespace Lunchbox;

public class ItemLunchBox : ILunchbox
{
    public ItemLunchBox() : base()
    {
        auto_eat_enabled = true;
    }
}