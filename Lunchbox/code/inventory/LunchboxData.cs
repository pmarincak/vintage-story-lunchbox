using Lunchbox.code.block_behaviour;
using Lunchbox.code.item;
using Lunchbox.code.utility;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Lunchbox.code.inventory;

/**
 * \brief Data representation of a lunchbox's current equip and inventory status.
 * \details 
 * Lunchbox data is split up into various other classes and data structures such as ItemStacks, EntityPlayer, InventoryBase, and so on.
 * The Lunchboxes require all of the data to be consolidated in order to implement the auto-eat, spoilage rate, and other lunchbox functionality.
 */
public class LunchboxData(string guid)
{
    // Constants
    public static readonly string HUNGER_KEY = "hunger"; //! Key for hunger-related statistics for the player
    public static readonly string THIRST_KEY = "thirst"; //! Key for thirst-related statistics for the player. Hydrate or Diedrate compatibility.
    public static readonly string LUNCHBOX_ID = "lunchbox_guid"; //! Key for tracking the lunchbox GUID value.

    // Members
    private EntityPlayer? _player_entity = null; //! Player entity that has the lunchbox equipped. If null then the box is not equipped.
    private InventoryBase? _inventory = null; //! Inventory of the player that has the lunchbox equipped. If null then the box is not equipped.
    private List<ItemSlotBagContent> _slots = []; //! Cached inventory slots of the player that has the lunchbox equipped. If empty then the box is not equipped.
    private ILunchbox? _lunchbox = null; //! Object type of the lunchbox we track.
    private readonly string _id = guid; //! ID of the tracked lunchbox.

    // Overrides
    public static bool operator !=(LunchboxData a, LunchboxData b)
    {
        return a._id != b._id;
    }

    public static bool operator ==(LunchboxData a, LunchboxData b)
    {
        return a._id == b._id;
    }

    // Setters

    /**
     * \brief Updates the \p lunchbox data with the current player \p inventory and \p bagContents.
     */
    public void UpdateData(ItemStack lunchbox, InventoryBase inventory, List<ItemSlotBagContent> bagContents)
    {
        var player = FoodItemUtility.GetPlayerOwnerFromInventory(inventory);

        // Order Important
        SetLunchbox((ILunchbox)lunchbox.Collectible);
        SetPlayerEntity(player);
        SetInventory(inventory);
        SetSlots(bagContents);
    }

    /**
     * \brief Sets the \p player that has the lunchbox currently equipped and configures that player for auto-eat functionality.
     * \note Logs when the lunchbox changes player ownership.
     */
    private void SetPlayerEntity(EntityPlayer? player)
    {
        var same = player == _player_entity;
        var old_entity_name = _player_entity != null ? _player_entity.GetName() : "null";
        var new_entity_name = player != null ? player.GetName() : "null";
        var autoeat = _lunchbox != null && _lunchbox.CanAutoEat();

        _player_entity?.WatchedAttributes.UnregisterListener(OnHungerChanged);
        _player_entity?.WatchedAttributes.UnregisterListener(OnThirstChanged);

        _player_entity = player;

        if (autoeat)
        {
            _player_entity?.WatchedAttributes.RegisterModifiedListener(HUNGER_KEY, OnHungerChanged);
            _player_entity?.WatchedAttributes.RegisterModifiedListener(THIRST_KEY, OnThirstChanged);
        }

        // We'll update because it's easier to keep everything up to date with the other objects but we'll only log when it changes for server owners
        if (!same)
        {
            LunchboxModSystem.Log("Changing Player Entity from [" + old_entity_name + "] to [" + new_entity_name + "]");
        }
    }

    private void SetLunchbox(ILunchbox? lunchbox)
    {
        _lunchbox = lunchbox;
    }

    private void SetInventory(InventoryBase? inventory)
    {
        _inventory = inventory;
    }

    public InventoryBase? GetInventory()
    {
        return _inventory;
    }

    /**
     * \brief Sets the cached inventory \p slots and configures the slots for spoilage rate functionality.
     */
    private void SetSlots(List<ItemSlotBagContent> slots)
    {

        _slots = slots;

        /* 
         * All backpacks share the same inventory
         * We need to flag that this item we've added to the slot belongs to a Lunchbox
         * Whether we tick for spoilage depends on if we have any Multipliers to apply
         */
        var behaviour = _lunchbox?.GetBehavior<CollectableBehaviorLunchbox>();
        foreach (ItemSlotBagContent slot in _slots)
        {
            behaviour?.AddTemporaryLunchboxID(slot.Itemstack);
        }
    }

    // Getters
    public EntityPlayer? GetPlayerEntity()
    {
        return _player_entity;
    }

    public List<ItemSlotBagContent> GetSlots()
    {
        return _slots;
    }

    // Auto-Eat Functionality
    private void OnHungerChanged()
    {
        if (_lunchbox == null || _player_entity == null || _inventory == null || _slots.Count == 0) return;

        AutoEatUtility.OnHungerChanged(this);
    }

    private void OnThirstChanged()
    {
        if (_lunchbox == null || _player_entity == null || _inventory == null || _slots.Count == 0) return;

        AutoEatUtility.OnThirstChanged(this);
    }
}