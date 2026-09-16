using System;

namespace Lunchbox.code.core
{
    public class Config
    {
        private static double MAXIMUM_SATIETY = 1500.0;

        public double minimum_satiety = 15.0; //! Minimum satiety that the player has before the lunchbox auto-eats
        public double minimum_thirst = 15.0; //! Minimum thirst that the player has before the lunchbox auto-drinks - hydrate or diedrate compatibility
        public bool cooler_autoeat_enabled = true; //! Whether the cooler auto-eats or not. For people who want perishrate multipliers without auto-eating.
        public bool temporal_backpack_autoeat_enabled = true; //! Whether the temporal backpack auto-eats or not. For people who want perishrate multipliers without auto-eating.
        public bool enable_logger = false; //! Whether to enable log messages or not.

        public void verify()
        {
            var minimum_trigger = MAXIMUM_SATIETY * 0.01; // 1%
            var maximum_trigger = MAXIMUM_SATIETY * 0.9; // 90%
            minimum_satiety = Math.Clamp(minimum_satiety, minimum_trigger, maximum_trigger);
            minimum_thirst = Math.Clamp(minimum_satiety, minimum_trigger, maximum_trigger);
        }
    }
}
