using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.Server;

namespace Lunchbox
{
    public class Config
    {
        public double minimum_satiety = 15.0; //! Minimum satiety that the player has before the lunchbox auto-eats
        public double minimum_thirst = 15.0; //! Minimum thirst that the player has before the lunchbox auto-drinks - hydrate or diedrate compatibility
        public bool cooler_autoeat_enabled = true; //! Whether the cooler auto-eats or not. For people who want perishrate multipliers without auto-eating.
        public bool temporal_backpack_autoeat_enabled = true; //! Whether the temporal backpack auto-eats or not. For people who want perishrate multipliers without auto-eating.

        public void verify()
        {
            // 1% to 90%
            minimum_satiety = Math.Clamp(minimum_satiety, 15.0, 1350.0);
            minimum_thirst = Math.Clamp(minimum_satiety, 15.0, 1350.0);
        }
    }
}
