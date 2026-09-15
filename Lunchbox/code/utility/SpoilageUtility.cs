using Vintagestory.API.Common;

namespace Lunchbox;

/**
 * \brief Contains various utilities used by the Lunchbox to determine food spoilage.
 */
public static class SpoilageUtility
{

    private static float DEFAULT_PERISHABLE_FACTOR = 1.0f;

    /**
     * \brief Returns whether the spoilage rate multiplier for the \p lunchbox.
     */
    public static float GetSpoilageRateMul(CollectibleObject? lunchbox)
    {
        return lunchbox?.Attributes?["defaultSpoilSpeedMult"].Exists == true ? lunchbox.Attributes["defaultSpoilSpeedMult"].AsFloat() : DEFAULT_PERISHABLE_FACTOR;
    }
}