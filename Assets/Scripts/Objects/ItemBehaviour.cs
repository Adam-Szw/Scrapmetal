using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static ContentGenerator;
using static CreatureBehaviour;

public class ItemBehaviour : EntityBehaviour, Saveable<ItemData>, Spawnable<ItemData>
{
    public FactionAllegiance ownerFaction = FactionAllegiance.neutral;
    public ItemTier tier = ItemTier.weak;
    public string inventoryIconLink = "Icons/Icon_Test";
    public int descriptionTextLinkID = 0;
    public int value = 0;
    public bool pickable = true;
    public bool removeOnPick = false;

    [HideInInspector] public ulong ownerID = 0;

    protected void Start()
    {
        if (pickable)
        {
            AddInteractionCollider();
            interactionEnterEffect = (CreatureBehaviour user) => { InteractionEnter(user); };
            interactionUseEffect = (CreatureBehaviour user) => { InteractionUse(user); };
        }
    }

    public virtual void Use() { }

    private void InteractionUse(CreatureBehaviour user)
    {
        // Send text if inventory is full and we cant pick up item
        if (user.GetInventory().Count >= inventoryLimit && !removeOnPick)
        {
            user.SpawnFloatingText(Color.red, "Inventory full!", 0.5f);
            return;
        }
        if (aura) Destroy(aura);
        aura = null;
        if (hText) Destroy(hText);
        hText = null;
        // Give the item to the user and destroy entity
        pickable = false;   // Make sure that item is no longer interactible while in inventory
        MethodInfo saveMethod = GetType().GetMethod("Save");
        ItemData item = (ItemData)saveMethod.Invoke(this, null);
        GiveItem(item, user);
        Destroy(gameObject);
    }

    private void InteractionEnter(CreatureBehaviour user)
    {
        // Do nothing if the user is not player
        if (user is not PlayerBehaviour) return;
        StartCoroutine(HighlightEntityCoroutine(PlayerBehaviour.interactibleInteravalTime));
        StartCoroutine(SpawnInteractionTextCoroutine("Press (E) to pick up item", PlayerBehaviour.interactibleInteravalTime, 0.7f));
    }

    // Give item to a creature with inventory
    public static void GiveItem(ItemData item, CreatureBehaviour receiver)
    {
        if (!item.removeOnPick)
        {
            receiver.GiveItem(item);
        }
        // Actions for scrap/currency or other things that get liquidated instantly
        else
        {
            // Add value to the player's currency count if picked up by player
            if (receiver is PlayerBehaviour) ((PlayerBehaviour)receiver).currencyCount += item.value;
            // Spawn text
            receiver.SpawnFloatingText(Color.green, "Scrap +" + item.value, 0.5f);
        }
    }

    public new ItemData Save()
    {
        ItemData data = new ItemData(base.Save());
        data.prefabPath = prefabPath;
        data.ownerID = ownerID;
        data.ownerFaction = ownerFaction;
        data.tier = tier;
        data.descriptionTextLinkID = descriptionTextLinkID;
        data.inventoryIconLink = inventoryIconLink;
        data.value = value;
        data.pickable = pickable;
        data.removeOnPick = removeOnPick;
        return data;
    }

    public void Load(ItemData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        ownerID = data.ownerID;
        ownerFaction = data.ownerFaction;
        tier = data.tier;
        descriptionTextLinkID = data.descriptionTextLinkID;
        inventoryIconLink = data.inventoryIconLink;
        value = data.value;
        pickable = data.pickable;
        removeOnPick = data.removeOnPick;
    }

    public static GameObject Spawn(ItemData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = EntityBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<ItemBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(ItemData data, Transform parent = null)
    {
        GameObject obj = EntityBehaviour.Spawn(data, parent);
        obj.GetComponent<ItemBehaviour>().Load(data);
        return obj;
    }

    // This spawn method will check if data is actually of one of its children and call them instead
    public static GameObject FlexibleSpawn(ItemData data, Transform parent = null)
    {
        GameObject obj;
        if (data is WeaponData) obj = WeaponBehaviour.Spawn((WeaponData)data, parent);
        else if (data is ArmorData) obj = ArmorBehaviour.Spawn((ArmorData)data, parent);
        else obj = Spawn(data, parent);
        obj.GetComponent<ItemBehaviour>().ID = ++GlobalControl.nextID;
        return obj;
    }
}

[Serializable]
public class ItemData : EntityData
{
    public ItemData() { }

    public ItemData(EntityData data)
    {
        prefabPath = data.prefabPath;
        ID = data.ID;
        location = data.location;
        rotation = data.rotation;
        scale = data.scale;
        velocity = data.velocity;
        speed = data.speed;
        active = data.active;
    }

    public ulong ownerID;
    public FactionAllegiance ownerFaction;
    public ItemTier tier;
    public string inventoryIconLink;
    public int descriptionTextLinkID;
    public int value;
    public bool pickable;
    public bool removeOnPick;
}