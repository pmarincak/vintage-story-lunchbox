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

public struct LunchboxData
{
    static public string HUNGER_KEY = "hunger"; //! Key for hunger-related statistics for the player
    static public string THIRST_KEY = "thirst"; //! Key for thirst-related statistics for the player. Hydrate or Diedrate compatibility.

    private EntityPlayer? _player_entity = null; //! Player entity that has the lunchbox equipped. If null then the box is not equipped.
    private InventoryBase? _inventory = null;
    private List<ItemSlotBagContent> _slots = new List<ItemSlotBagContent>();
    private CollectibleObject _lunchbox_obj = null;

    public LunchboxData(CollectibleObject obj) { _lunchbox_obj = obj; }

    public void SetPlayerEntity(EntityPlayer? player)
    {
        var old_entity_name = _player_entity != null ? _player_entity.GetName() : "null";
        var new_entity_name = player != null ? player.GetName() : "null";
        
        _player_entity?.WatchedAttributes.UnregisterListener(OnHungerChanged);
        //_player_entity?.WatchedAttributes.UnregisterListener(OnThirstChanged);

        _player_entity = player;

        player?.WatchedAttributes.RegisterModifiedListener(HUNGER_KEY, OnHungerChanged);
        //player?.WatchedAttributes.RegisterModifiedListener(THIRST_KEY, OnThirstChanged);

        LunchboxModSystem.Log("Changing Player Entity from [" + old_entity_name + "] to [" + new_entity_name + "]");
    }

    public void OnHungerChanged()
    {
        ((ILunchbox)_lunchbox_obj)?.OnHungerChanged(this);
    }

    public EntityPlayer? GetPlayerEntity()
    {
        return _player_entity;
    }

    public void SetInventory(InventoryBase? inventory, CustomGetTransitionSpeedMulDelegate Inventory_OnAcquireTransitionSpeed)
    {
        if (_inventory != null)
        {
            _inventory.OnAcquireTransitionSpeed -= Inventory_OnAcquireTransitionSpeed;
        }

        _inventory = inventory;

        if (_inventory != null)
        {
            _inventory.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
        }
    }

    public void SetSlots(List<ItemSlotBagContent> slots)
    {
        _slots = slots;
    }

    public List<ItemSlotBagContent> GetSlots()
    {
        return _slots;
    }
}

class CollectableBehaviorLunchbox : CollectibleBehaviorHeldBag, IHeldBag
{
    // Generic
    static private string LUNCHBOX_ID = "lunchbox_guid";
    private Dictionary<string, LunchboxData> _server_lunchbox_tracking = new Dictionary<string, LunchboxData>();
    private Type _slot_type;
    
    // Spoilage
    private static float DEFAULT_PERISHABLE_FACTOR = 1.0f;

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

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        var perish_rate = GetSpoilageRateMul(inSlot.Itemstack);

        // This info technically lives on the Lunchbox but the order looks strange so we'll put it here
        dsc.AppendLine(Lang.Get("Stored food perish speed: {0}x", Math.Round(perish_rate, 2)));
    }

    public LunchboxData GetLunchboxData(ItemStack lunchbox)
    {
        var guid = lunchbox.Attributes.GetAsString(LUNCHBOX_ID, null);
        var data = _server_lunchbox_tracking.Get(guid);
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
        ConfigureAutoEat(world, bagstack, parentinv, bagContents);

        return bagContents;
    }

    private void ConfigureAutoEat(IWorldAccessor world, ItemStack lunchbox, InventoryBase inventory, List<ItemSlotBagContent> bagContents)
    {
        if (!(world is IServerWorldAccessor))
        {
            return;
        }

        // Get the GUID or Assign if not exists
        var guid = lunchbox.Attributes.GetAsString(LUNCHBOX_ID, null);
        if (guid == null)
        {
            guid = Guid.NewGuid().ToString();
            // Assign the GUID so it persists between saves / copies
            lunchbox.Attributes.SetString(LUNCHBOX_ID, guid);

            LunchboxModSystem.Log("Adding GUID [" + guid + "] to [" + lunchbox.GetName() + "]");
        }

        // Add Tracking Status
        if (!_server_lunchbox_tracking.ContainsKey(guid))
        {
            LunchboxModSystem.Log("Adding Lunchbox Tracking for [" + lunchbox.GetName() + "-" + guid + "]");
            _server_lunchbox_tracking.Add(guid, new LunchboxData(lunchbox.Collectible));
        }

        // Update the tracking data
        var data = _server_lunchbox_tracking.Get(guid);
        var player = FoodItemUtility.GetPlayerOwnerFromInventory(inventory);

        if (data.GetPlayerEntity() == player)
        {
            // We are updating the same player let's return
            //return;
        }

        data.SetPlayerEntity(player);
        data.SetInventory(inventory, Inventory_OnAcquireTransitionSpeed);

        /*
         * Cache the bagContents before we return because otherwise we cannot access the created slots 
         * for the lunchbox implementation without recreating the slots and we want them to match
         */
        data.SetSlots(bagContents);

        _server_lunchbox_tracking[guid] = data;

        /*
         * If we've updated tracking let's update the auto eat
         * There seems to be no way to register the lunchbox auto-eat functionality on server connection. 
         * The only place that seems doable is here when the inventory slots are created.
         */
        ((ILunchbox)lunchbox.Collectible)?.ConfigureAutoEat(world, lunchbox, data);
    }

    // SPOILAGE

    /**
     * \brief Returns a transition speed modification.
     */
    private float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
    {
        // If it's invalid skip
        if (transType != EnumTransitionType.Perish) return 1;
        if (stack == null || stack.Collectible == null) return 1;

        // If it's not a lunchbox - skip
        if (!stack.Attributes.HasAttribute(LUNCHBOX_ID)) return 1;

        // No support for per-food-category perish rate yet
        return baseMul * GetSpoilageRateMul(stack);
    }

    /**
     * \brief Returns whether the spoilage rate multiplier for the \p lunchbox.
     */
    private float GetSpoilageRateMul(ItemStack lunchbox)
    {
        var lunchbox_obj = lunchbox.Collectible;
        return lunchbox_obj?.Attributes?["defaultSpoilSpeedMult"].Exists == true ? lunchbox_obj.Attributes["defaultSpoilSpeedMult"].AsFloat() : DEFAULT_PERISHABLE_FACTOR;
    }
}