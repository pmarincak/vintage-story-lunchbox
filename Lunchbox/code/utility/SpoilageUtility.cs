using Vintagestory.API.Common;

namespace Lunchbox;

/**
 * \brief Contains various utilities used by the Lunchbox to determine food spoilage.
 */
public static class SpoilageUtility
{

    private static float DEFAULT_PERISHABLE_FACTOR = 1.0f;

    /**
    * \brief Returns a transition speed modification.
    */
    public static float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
    {
        // If it's invalid skip
        if (transType != EnumTransitionType.Perish) return 1;
        if (stack == null || stack.Collectible == null) return 1;

        // If it's not a lunchbox - skip
        if (!stack.Attributes.HasAttribute(LunchboxData.LUNCHBOX_ID)) return 1;

        // No support for per-food-category perish rate yet
        return baseMul * GetSpoilageRateMul(stack);
    }

    /**
     * \brief Returns whether the spoilage rate multiplier for the \p lunchbox.
     */
    public static float GetSpoilageRateMul(ItemStack lunchbox)
    {
        var lunchbox_obj = lunchbox.Collectible;
        return lunchbox_obj?.Attributes?["defaultSpoilSpeedMult"].Exists == true ? lunchbox_obj.Attributes["defaultSpoilSpeedMult"].AsFloat() : DEFAULT_PERISHABLE_FACTOR;
    }
}