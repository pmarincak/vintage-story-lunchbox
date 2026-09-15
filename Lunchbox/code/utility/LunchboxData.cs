using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Lunchbox;

public class LunchboxData
{
    static public string HUNGER_KEY = "hunger"; //! Key for hunger-related statistics for the player
    static public string THIRST_KEY = "thirst"; //! Key for thirst-related statistics for the player. Hydrate or Diedrate compatibility.
    static public string LUNCHBOX_ID = "lunchbox_guid";

    private EntityPlayer? _player_entity = null; //! Player entity that has the lunchbox equipped. If null then the box is not equipped.
    private InventoryBase? _inventory = null;
    private List<ItemSlotBagContent> _slots = new List<ItemSlotBagContent>();
    private ILunchbox? _lunchbox = null;
    private string _id = "";

    public LunchboxData(string guid) {
        _id = guid;
    }

    public static bool operator!= (LunchboxData a, LunchboxData b)
    {
        return a._id != b._id;
    }

    public static bool operator == (LunchboxData a, LunchboxData b)
    {
        return a._id == b._id;
    }

    public void UpdateData(ItemStack lunchbox, InventoryBase inventory, List<ItemSlotBagContent> bagContents)
    {
        var player = FoodItemUtility.GetPlayerOwnerFromInventory(inventory);

        SetPlayerEntity(player);
        SetLunchbox((ILunchbox) lunchbox.Collectible);
        SetInventory(inventory);

        /*
         * Cache the bagContents before we return because otherwise we cannot access the created slots 
         * for the lunchbox implementation without recreating the slots and we want them to match
         */
        SetSlots(bagContents);
    }

    private void SetPlayerEntity(EntityPlayer? player)
    {
        var old_entity_name = _player_entity != null ? _player_entity.GetName() : "null";
        var new_entity_name = player != null ? player.GetName() : "null";

        _player_entity?.WatchedAttributes.UnregisterListener(OnHungerChanged);
        //_player_entity?.WatchedAttributes.UnregisterListener(OnThirstChanged);

        _player_entity = player;

        _player_entity?.WatchedAttributes.RegisterModifiedListener(HUNGER_KEY, OnHungerChanged);
        //player?.WatchedAttributes.RegisterModifiedListener(THIRST_KEY, OnThirstChanged);

        LunchboxModSystem.Log("Changing Player Entity from [" + old_entity_name + "] to [" + new_entity_name + "]");
    }

    private void SetLunchbox(ILunchbox? lunchbox)
    {
        _lunchbox = lunchbox;
    }

    private void OnHungerChanged()
    {
        _lunchbox?.OnHungerChanged(this);
    }

    public EntityPlayer? GetPlayerEntity()
    {
        return _player_entity;
    }

    private void SetInventory(InventoryBase? inventory)
    {
        if (_inventory != null)
        {
            _inventory.OnAcquireTransitionSpeed -= SpoilageUtility.Inventory_OnAcquireTransitionSpeed;
        }

        _inventory = inventory;

        if (_inventory != null)
        {
            _inventory.OnAcquireTransitionSpeed += SpoilageUtility.Inventory_OnAcquireTransitionSpeed;
        }
    }

    private void SetSlots(List<ItemSlotBagContent> slots)
    {
        _slots = slots;
    }

    public List<ItemSlotBagContent> GetSlots()
    {
        return _slots;
    }
}