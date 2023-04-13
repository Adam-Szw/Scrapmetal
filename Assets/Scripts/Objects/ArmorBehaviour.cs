using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerBehaviour;

public class ArmorBehaviour : ItemBehaviour, Saveable<ArmorData>, Spawnable<ArmorData>
{

    public ArmorSlot.Slot slot;

    protected new void Awake()
    {
        base.Awake();
    }

    protected new void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
    }
    public new ArmorData Save()
    {
        ArmorData data = new ArmorData(base.Save());
        data.slot = slot;
        return data;
    }

    public void Load(ArmorData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        this.slot = data.slot;
    }

    public static GameObject Spawn(ArmorData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<WeaponBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(ArmorData data, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, parent);
        obj.GetComponent<WeaponBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class ArmorData : ItemData
{
    public ArmorData() { }

    public ArmorData(ItemData data) : base(data)
    {
        this.prefabPath = data.prefabPath;
        this.ownerID = data.ownerID;
        this.descriptionText = data.descriptionText;
        this.inventoryIconLink = data.inventoryIconLink;
        this.value = data.value;
    }

    public ArmorSlot.Slot slot;

}