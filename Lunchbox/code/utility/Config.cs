using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.Server;

namespace Lunchbox
{
    public class Config
    {
        public double minimum_satiety = 15.0; //! Minimum satiety that the player has before the lunchbox auto-eats

        public void verify()
        {
            // 1% to 90%
            minimum_satiety = Math.Clamp(minimum_satiety, 15.0, 1350.0);
        }
    }
}
