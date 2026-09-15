using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static System.Runtime.InteropServices.JavaScript.JSType;

#nullable disable

namespace Lunchbox;

class CollectableBehaviorLunchbox : CollectibleBehaviorHeldBag, IHeldBag
{
    // Generic
    private Dictionary<string, LunchboxData> _server_lunchbox_tracking = new Dictionary<string, LunchboxData>();
    private Type _slot_type;

    public CollectableBehaviorLunchbox(CollectibleObject obj) : base(obj)
    {
    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        ParseSlotType(properties["slotType"].AsString(""));
    }

    /**
     * \brief Parses the provided slot \a type to use for creating slots.
     * \sa GetOrCreateSlots
     */
    private void ParseSlotType(string type)
    {
        if (type == "Generic")
        {
            _slot_type = typeof(ItemSlotBagContent);
        }
        else {
            _slot_type = typeof(FoodSlot);
        }
    }

    public new void Store(ItemStack bagstack, ItemSlotBagContent slot)
    {
        base.Store(bagstack, slot);

        var data = GetLunchboxData(bagstack);
        data.AddTemporaryLunchboxID(slot?.Itemstack);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        var perish_rate = SpoilageUtility.GetSpoilageRateMul(inSlot.Itemstack?.Collectible);

        // This info technically lives on the Lunchbox but the order looks strange so we'll put it here
        dsc.AppendLine(Lang.Get("Stored food perish speed: {0}x", Math.Round(perish_rate, 2)));
    }

    public LunchboxData GetLunchboxData(ItemStack lunchbox)
    {
        var guid = lunchbox.Attributes.GetAsString(LunchboxData.LUNCHBOX_ID, null);
        var data = _server_lunchbox_tracking.Get(guid, new LunchboxData(""));
        return data;
    }

    /*
     * Copy of CollectibleBehaviorHeldBag::GetOrCreateSlots but ItemSlotBagContent slots are initialized to FoodSlot 
     * because we need to specify a new ItemSlot for this bag to hold and there is no nice way to do it and we can't override the parent.
     */
    public new List<ItemSlotBagContent> GetOrCreateSlots(ItemStack bagstack, InventoryBase parentinv, int bagIndex, IWorldAccessor world)
    {
        var bagContents = new List<ItemSlotBagContent>();

        string bgcolhex = GetSlotBgColor(bagstack);
        var flags = GetStorageFlags(bagstack);
        int quantitySlots = GetQuantitySlots(bagstack);

        ITreeAttribute stackBackPackTree = bagstack.Attributes.GetTreeAttribute("backpack");
        if (stackBackPackTree == null)
        {
            stackBackPackTree = new TreeAttribute();
            ITreeAttribute slotsTree = new TreeAttribute();

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
            }
        }

        // Registers the tracking for the Lunchbox and configures the auto eat functionality if necessary
        /*
         * Cache the bagContents before we return because otherwise we cannot access the created slots 
         * for the lunchbox implementation without recreating the slots and we want them to match
         */
        ConfigureAutoEat(world, bagstack, parentinv, bagContents);

        return bagContents;
    }

    public void ConfigureAutoEat(IWorldAccessor world, ItemStack lunchbox, InventoryBase inventory, List<ItemSlotBagContent> bagContents)
    {
        if (!(world is IServerWorldAccessor))
        {
           return;
        }

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
}