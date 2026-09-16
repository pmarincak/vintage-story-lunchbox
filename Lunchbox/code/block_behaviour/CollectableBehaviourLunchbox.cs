using Lunchbox.code.inventory;
using Lunchbox.code.utility;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Lunchbox.code.block_behaviour;

class CollectableBehaviorLunchbox(CollectibleObject obj) : CollectibleBehaviorHeldBag(obj), IHeldBag
{
    // Constants
    private static readonly string LUNCHBOX_BEHAVIOUR_ID = "lunchbox_behaviour_guid";

    // Generic
    private readonly string _behaviour_guid = Guid.NewGuid().ToString();
    private Dictionary<string, LunchboxData> _server_lunchbox_tracking = [];
    private Type _slot_type = typeof(FoodSlot);
    private float _spoilage_modifier = 1.0f;

    // Overrides
    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        var type = properties["slotType"].AsString("");
        if (type == "Generic")
        {
            _slot_type = typeof(ItemSlotBagContent);
        }
    }

    // Force an override on Store so that we can set IDs for spoilage rate modification
    public new void Store(ItemStack bagstack, ItemSlotBagContent slot)
    {
        base.Store(bagstack, slot);

        AddTemporaryLunchboxID(slot?.Itemstack);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        // Spoilage Rates are set onto the object we provide a behaviour for
        // Each object has an instance of a Collectable Behaviour
        // We will cache the spoilage rate from the object we modify for later use
        // This value isn't being displayed to the user in the menu
        // For some reason it isn't set properly in the constructor
        // Quick Hack
        _spoilage_modifier = SpoilageUtility.GetSpoilageRateMul(inSlot?.Itemstack?.Collectible);

        // This info technically lives on the Lunchbox but the order looks strange so we'll put it here
        dsc.AppendLine(Lang.Get("Stored food perish speed: {0}x", Math.Round(_spoilage_modifier, 2)));
    }

    /*
     * Copy of CollectibleBehaviorHeldBag::GetOrCreateSlots but ItemSlotBagContent slots are initialized to FoodSlot 
     * because we need to specify a new ItemSlot for this bag to hold and there is no nice way to do it and we can't override the parent.
     */
    public new List<ItemSlotBagContent> GetOrCreateSlots(ItemStack bagstack, InventoryBase parentinv, int bagIndex, IWorldAccessor world)
    {
        // If we don't assign this here Perish Mult won't work :(
        // May work in some other hook but for now it lives here
        _spoilage_modifier = SpoilageUtility.GetSpoilageRateMul(bagstack?.Collectible);

        var bagContents = new List<ItemSlotBagContent>();

        string bgcolhex = GetSlotBgColor(bagstack);
        var flags = GetStorageFlags(bagstack);
        int quantitySlots = GetQuantitySlots(bagstack);

        ITreeAttribute stackBackPackTree = bagstack.Attributes.GetTreeAttribute("backpack");
        if (stackBackPackTree == null)
        {
            stackBackPackTree = new TreeAttribute();
            TreeAttribute slotsTree = new();

            for (int slotIndex = 0; slotIndex < quantitySlots; slotIndex++)
            {
                ItemSlotBagContent slot = (ItemSlotBagContent)Activator.CreateInstance(_slot_type, parentinv, bagIndex, slotIndex, flags); // Change
                slot.HexBackgroundColor = bgcolhex;
                bagContents.Add(slot);
                slotsTree["slot-" + slotIndex] = new ItemstackAttribute(null);
            }

            stackBackPackTree["slots"] = slotsTree;
            bagstack.Attributes["backpack"] = stackBackPackTree;
        }
        else
        {
            ITreeAttribute slotsTree = stackBackPackTree.GetTreeAttribute("slots");

            foreach (var val in slotsTree)
            {
                int slotIndex = val.Key.Split("-")[1].ToInt();
                ItemSlotBagContent slot = (ItemSlotBagContent)Activator.CreateInstance(_slot_type, parentinv, bagIndex, slotIndex, flags); // Change
                slot.HexBackgroundColor = bgcolhex;

                if (val.Value?.GetValue() != null)
                {
                    ItemstackAttribute attr = (ItemstackAttribute)val.Value;
                    slot.Itemstack = attr.value;
                    slot.Itemstack.ResolveBlockOrItem(world);
                }

                while (bagContents.Count <= slotIndex) bagContents.Add(null);
                bagContents[slotIndex] = slot;
                AddTemporaryLunchboxID(slot.Itemstack);
            }
        }

        /*
         * Cache the bagContents before we return because otherwise we cannot access the created slots 
         * for the lunchbox implementation without recreating the slots and we want them to match
         */
        ConfigureAutoEat(bagstack, parentinv, bagContents); // Change

        return bagContents;
    }

    // Lunchbox

    /**
    * \brief Parses the provided slot \p type to use for creating slots.
    * \sa GetOrCreateSlots
    */
    public LunchboxData GetLunchboxData(ItemStack lunchbox)
    {
        var guid = lunchbox.Attributes.GetAsString(LunchboxData.LUNCHBOX_ID, null);
        var data = _server_lunchbox_tracking.Get(guid, new LunchboxData(""));
        return data;
    }

    // 
    /**
    * \brief Registers the data tracking for the \p lunchbox using it's \p bagContents for the player \p inventory.
    * \note Configures the auto eat functionality if necessary.
    * \note Lazy loads and logs the tracking information as needed.
    */
    public void ConfigureAutoEat(ItemStack lunchbox, InventoryBase inventory, List<ItemSlotBagContent> bagContents)
    {
        // Get the GUID or Assign if not exists
        var guid = lunchbox.Attributes.GetAsString(LunchboxData.LUNCHBOX_ID, null);
        if (guid == null)
        {
            guid = Guid.NewGuid().ToString();
            // Assign the GUID so it persists between saves / copies
            lunchbox.Attributes.SetString(LunchboxData.LUNCHBOX_ID, guid);

            LunchboxModSystem.Log("Adding GUID [" + guid + "] to [" + lunchbox.GetName() + "]");
        }

        // Add Tracking Status
        if (!_server_lunchbox_tracking.ContainsKey(guid))
        {
            LunchboxModSystem.Log("Adding Lunchbox Tracking for [" + lunchbox.GetName() + "-" + guid + "]");
            _server_lunchbox_tracking.Add(guid, new LunchboxData(guid));
        }

        // Update the tracking data
        var data = _server_lunchbox_tracking[guid];
        data.UpdateData(lunchbox, inventory, bagContents);
        _server_lunchbox_tracking[guid] = data;
    }

    /**
    * \brief Returns a transition speed modification for spoilage rate functionality.
    */
    public float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
    {
        // If it's invalid skip
        if (transType != EnumTransitionType.Perish) return 1;
        if (stack == null || stack.Collectible == null) return 1;

        // All backpacks share the same inventory
        // If it's not managed by this behaviour then skip
        if (stack.TempAttributes.GetString(LUNCHBOX_BEHAVIOUR_ID, "") != _behaviour_guid) return 1;

        // No support for per-food-category perish rate yet
        return baseMul * _spoilage_modifier;
    }

    /**
    * \brief Sets a temporary attribute on the \p item for spoilage rate functionality.
    */
    public void AddTemporaryLunchboxID(ItemStack? item)
    {
        item?.TempAttributes.SetString(LUNCHBOX_BEHAVIOUR_ID, _behaviour_guid);
    }
}