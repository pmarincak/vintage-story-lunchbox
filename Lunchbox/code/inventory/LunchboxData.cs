using Lunchbox.code.item;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Lunchbox.code.inventory;

public class LunchboxData
{
    static public string HUNGER_KEY = "hunger"; //! Key for hunger-related statistics for the player
    static public string THIRST_KEY = "thirst"; //! Key for thirst-related statistics for the player. Hydrate or Diedrate compatibility.
    static public string LUNCHBOX_ID = "lunchbox_guid";

    private EntityPlayer? _player_entity = null; //! Player entity that has the lunchbox equipped. If null then the box is not equipped.
    public InventoryBase? _inventory = null;
    private List<ItemSlotBagContent> _slots = new List<ItemSlotBagContent>();
    private ILunchbox? _lunchbox = null;
    private string _id = "";

    public LunchboxData(string guid) {
        _id = guid;
    }

    public static bool operator !=(LunchboxData a, LunchboxData b)
    {
        return a._id != b._id;
    }

    public static bool operator ==(LunchboxData a, LunchboxData b)
    {
        return a._id == b._id;
    }

    public void UpdateData(ItemStack lunchbox, InventoryBase inventory, List<ItemSlotBagContent> bagContents)
    {
        var player = FoodItemUtility.GetPlayerOwnerFromInventory(inventory);

        // Order Important
        SetLunchbox((ILunchbox)lunchbox.Collectible);
        SetPlayerEntity(player);
        SetInventory(inventory);
        SetSlots(bagContents);
    }

    private void SetPlayerEntity(EntityPlayer? player)
    {
        var same = player == _player_entity;
        var old_entity_name = _player_entity != null ? _player_entity.GetName() : "null";
        var new_entity_name = player != null ? player.GetName() : "null";
        var autoeat = _lunchbox != null ? _lunchbox.CanAutoEat() : false;

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

    private void OnHungerChanged()
    {
        if (_lunchbox == null ||  _player_entity == null || _inventory == null || _slots.Count == 0) return;

        AutoEatUtility.OnHungerChanged(this);
    }

    private void OnThirstChanged()
    {
        if (_lunchbox == null || _player_entity == null || _inventory == null || _slots.Count == 0) return;

        AutoEatUtility.OnThirstChanged(this);
    }

    public EntityPlayer? GetPlayerEntity()
    {
        return _player_entity;
    }

    private void SetInventory(InventoryBase? inventory)
    {
        var behaviour = _lunchbox?.GetBehavior<CollectableBehaviorLunchbox>();

        if (_inventory != null && behaviour != null)
        {
            _inventory.OnAcquireTransitionSpeed -= behaviour.Inventory_OnAcquireTransitionSpeed;
        }

        _inventory = inventory;

        if (_inventory != null && behaviour != null)
        {
            _inventory.OnAcquireTransitionSpeed += behaviour.Inventory_OnAcquireTransitionSpeed;
        }
    }

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

    public List<ItemSlotBagContent> GetSlots()
    {
        return _slots;
    }
}