using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

#nullable disable

namespace Lunchbox;

class CollectableBehaviorLunchbox : CollectibleBehaviorHeldBag, IHeldBag
{
    // Generic
    public List<ItemSlotBagContent> _slots;
    public InventoryBase _inventory;
    CollectibleObject _lunchbox;
    private Type _slot_type;

    // Spoilage
    private float _defaultPerishableFactor = 1.0f;
    static private string LUNCHBOX_ATTRIBUTE = "lunchbox";

    public CollectableBehaviorLunchbox(CollectibleObject obj) : base(obj)
    {
        _lunchbox = obj;
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
        AddLunchboxAttribute(slot?.Itemstack);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        // This value isn't being displayed to the user in the menu
        // For some reason it isn't set properly in the constructor
        // Quick Hack
        _defaultPerishableFactor = _lunchbox?.Attributes?["defaultSpoilSpeedMult"].Exists == true ? _lunchbox.Attributes["defaultSpoilSpeedMult"].AsFloat() : _defaultPerishableFactor;

        // This info technically lives on the Lunchbox but the order looks strange so we'll put it here
        dsc.AppendLine(Lang.Get("Stored food perish speed: {0}x", Math.Round(_defaultPerishableFactor, 2)));
    }

    /*
     * Copy of CollectibleBehaviorHeldBag::GetOrCreateSlots but ItemSlotBagContent slots are initialized to FoodSlot 
     * because we need to specify a new ItemSlot for this bag to hold and there is no nice way to do it and we can't override the parent.
     */
    public new List<ItemSlotBagContent> GetOrCreateSlots(ItemStack bagstack, InventoryBase parentinv, int bagIndex, IWorldAccessor world)
    {
        // If we don't assign this here Perish Mult won't work :(
        // May work in some other hook but for now it lives here
        _defaultPerishableFactor = _lunchbox?.Attributes?["defaultSpoilSpeedMult"].Exists == true ? _lunchbox.Attributes["defaultSpoilSpeedMult"].AsFloat() : _defaultPerishableFactor;

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
                AddLunchboxAttribute(slot.Itemstack); // Change
            }
        }

        /*
         * Cache the bagContents before we return because otherwise we cannot access the created slots 
         * for the lunchbox implementation without recreating the slots and we want them to match
         */
        _slots = bagContents;

        /*
         * There seems to be no way to register the lunchbox auto-eat functionality on server connection. 
         * The only place that seems doable is here when the inventory slots are created.
         */
        ((ILunchbox)_lunchbox)?.ConfigureAutoEat(world, parentinv);

        /*
         * Register Spoilage Rate Multiplier to the Inventory
         */
        if (HasSpoilageRateMul())
        {
            if (_inventory != null)
            {
                _inventory.OnAcquireTransitionSpeed -= Inventory_OnAcquireTransitionSpeed;
            }

            _inventory = parentinv;
            _inventory.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
        }

        return bagContents;
    }

    /**
     * \brief Returns a transition speed modification.
     */
    private float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
    {
        // If it's invalid skip
        if (transType != EnumTransitionType.Perish) return 1;
        if (stack == null || stack.Collectible == null) return 1;

        // If it's not in this Lunchbox skip
        if (stack.TempAttributes.GetInt(LUNCHBOX_ATTRIBUTE, -1) != _lunchbox.Id) return 1;

        // No support for per-food-category perish rate yet
        return baseMul * _defaultPerishableFactor;
    }

    /**
     * \brief Appends a temporary attribute to the provided item \a stack that it is stored in a lunchbox.
     */
    private void AddLunchboxAttribute(ItemStack stack)
    {
        /* 
         * All backpacks share the same inventory
         * We need to flag that this item we've added to the slot belongs to a Lunchbox
         * Whether we tick for spoilage depends on if we have any Multipliers to apply
         */
        if (HasSpoilageRateMul())
        {
            stack?.TempAttributes.SetInt(LUNCHBOX_ATTRIBUTE, _lunchbox.Id);
        }
    }

    /**
     * \brief Returns whether this has a spoilage rate multiplier or not.
     */
    private bool HasSpoilageRateMul()
    {
        return _defaultPerishableFactor != 1.0f;
    }
}