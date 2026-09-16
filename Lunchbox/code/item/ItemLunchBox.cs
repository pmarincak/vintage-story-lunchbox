using Lunchbox.code.item;

namespace Lunchbox;

public class ItemLunchBox : ILunchbox
{
    public ItemLunchBox() : base()
    {
    }

    public override bool CanAutoEat() { return true; }
}