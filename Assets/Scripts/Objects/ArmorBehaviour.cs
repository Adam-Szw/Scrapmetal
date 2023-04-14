using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ArmorBehaviour;
using static HumanoidAnimations;
using static PlayerBehaviour;

public class ArmorBehaviour : ItemBehaviour, Saveable<ArmorData>, Spawnable<ArmorData>
{

    [Serializable]
    public struct ArmorSlot
    {
        public enum Slot
        {
            head, torso, arms, legs
        }

        public ArmorData armor;
        public Slot slot;

        public ArmorSlot(ArmorData armor, Slot slot)
        {
            this.armor = armor;
            this.slot = slot;
        }
    }

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

    public static ArmorData Produce(string prefabPath, ulong descriptionLink, string iconLink, int value, bool pickable,
     ArmorSlot.Slot slot)
    {
        ArmorData data = new ArmorData(ItemBehaviour.Produce(prefabPath, descriptionLink, iconLink, value, pickable));
        data.slot = slot;
        return null;
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
        prefabPath = data.prefabPath;
        ownerID = data.ownerID;
        descriptionTextLinkID = data.descriptionTextLinkID;
        inventoryIconLink = data.inventoryIconLink;
        value = data.value;
    }

    public ArmorSlot.Slot slot;

}