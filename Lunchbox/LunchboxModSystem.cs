using System;
using Vintagestory.API.Common;

namespace Lunchbox;

public class LunchboxModSystem : ModSystem
{
    public static Config config { get; private set; } = null!;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        TryToLoadConfig(api);

        api.RegisterCollectibleBehaviorClass("Lunchbox.LunchboxBehaviour", typeof(CollectableBehaviorLunchbox));
        api.RegisterItemClass("Lunchbox.LunchboxItem", typeof(ItemLunchBox));
        api.RegisterItemClass("Lunchbox.CoolerItem", typeof(ItemCooler));
    }

    private void TryToLoadConfig(ICoreAPI api)
    {
        if (api.Side != EnumAppSide.Server)
        {
            return;
        }

        try
        {
            var config_file_name = "lunchbox_config.json";
            config = api.LoadModConfig<Config>(config_file_name);

            if (config == null)
            {
                config = new Config();
            }

            config.verify();
            api.StoreModConfig<Config>(config, config_file_name);
        }
        catch (Exception e)
        {
            //Couldn't load the mod config... Create a new one with default settings, but don't save it.
            Mod.Logger.Error("Could not load config! Loading default settings instead.");
            Mod.Logger.Error(e);
            config = new Config();
        }
    }
}
