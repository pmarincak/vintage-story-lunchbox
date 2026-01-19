using Vintagestory.API.Common;

namespace Lunchbox;

public class LunchboxModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        api.RegisterCollectibleBehaviorClass("Lunchbox.LunchboxBehaviour", typeof(CollectableBehaviorLunchbox));
        api.RegisterItemClass("Lunchbox.LunchboxItem", typeof(ItemLunchBox));
        api.RegisterBlockClass("Lunchbox.LunchboxBlock", typeof(BlockLunchBox));
    }

}
