using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static WeaponBehaviour;

public class ItemBehaviour : EntityBehaviour, Saveable<ItemData>, Spawnable<ItemData>
{
    public ulong ownerID = 0;
    public CreatureBehaviour.FactionAllegiance ownerFaction = CreatureBehaviour.FactionAllegiance.berserk;

    public string descriptionText = "No description";
    public string inventoryIconLink = "Icons/Icon_Test";
    public int value = 10;

    public virtual void Use() { }

    public new ItemData Save()
    {
        ItemData data = new ItemData(base.Save());
        data.prefabPath = prefabPath;
        data.ownerID = ownerID;
        data.ownerFaction = ownerFaction;
        data.descriptionText = descriptionText;
        data.inventoryIconLink = inventoryIconLink;
        data.value = value;
        return data;
    }

    public void Load(ItemData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        ownerID = data.ownerID;
        ownerFaction = data.ownerFaction;
        descriptionText = data.descriptionText;
        inventoryIconLink = data.inventoryIconLink;
        value = data.value;
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
    }

    public ulong ownerID;
    public CreatureBehaviour.FactionAllegiance ownerFaction;
    public string descriptionText;
    public string inventoryIconLink;
    public int value;
}