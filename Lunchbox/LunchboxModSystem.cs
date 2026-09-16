using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Lunchbox;

public class LunchboxModSystem : ModSystem
{
    public static Config config { get; private set; } = null!;
    private static ILogger? logger;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        TryToLoadConfig(api);

        api.RegisterCollectibleBehaviorClass("Lunchbox.LunchboxBehaviour", typeof(CollectableBehaviorLunchbox));
        api.RegisterItemClass("Lunchbox.LunchboxItem", typeof(ItemLunchBox));
        api.RegisterItemClass("Lunchbox.CoolerItem", typeof(ItemCooler));
        api.RegisterItemClass("Lunchbox.TemporalBackpackItem", typeof(ItemTemporalBackpack));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);

        logger = Mod?.Logger;
    }

    /**
     * \brief Tries to load the mod config.
     * \note If the configuration file does not exist it will create a new file.
     * \note On the server the newly created file will be saved.
     */
    private void TryToLoadConfig(ICoreAPI api)
    {
        if (api.Side != EnumAppSide.Server)
        {
            config = new Config();
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

    /**
     * \brief Prints the \p message to the server debug log.
     */
    public static void Log(String message)
    {
        if (config.enable_logger)
        {
            logger?.Debug(message);
        }
    }

    /**
     * \brief Prints the \p message to the server debug log along with the \p player name, if applicable.
     */
    public static void Log(EntityPlayer? player, String message)
    {
        if (player == null)
        {
            Log(message);
        }
        else
        {
            Log("[" + player.GetName() + "] " + message);
        }
    }
}
