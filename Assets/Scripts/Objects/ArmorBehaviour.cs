using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ArmorBehaviour;
using static HumanoidAnimations;
using static PlayerBehaviour;

public class ArmorBehaviour : ItemBehaviour, Saveable<ArmorData>, Spawnable<ArmorData>
{
    // Stats data
    public float hpIncrease = 0f;
    public float speedMultiplierBonus = 0f;
    public bool buffsScrapGeneration = false;
    public bool buffsLootGeneration = false;
    public bool buffsChestOpening = false;

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

    // Graphical data - where the armor goes and which sprite from library to use
    public ArmorSlot.Slot slot;
    public int labelIndex;
    public string colorRGBA;

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
        data.hpIncrease = hpIncrease;
        data.speedMultiplierBonus = speedMultiplierBonus;
        data.buffsScrapGeneration = buffsScrapGeneration;
        data.buffsLootGeneration = buffsLootGeneration;
        data.buffsChestOpening = buffsChestOpening;
        data.labelIndex = labelIndex;
        data.colorRGBA = colorRGBA;
        return data;
    }

    public void Load(ArmorData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        slot = data.slot;
        hpIncrease = data.hpIncrease;
        speedMultiplierBonus = data.speedMultiplierBonus;
        buffsScrapGeneration = data.buffsScrapGeneration;
        buffsLootGeneration = data.buffsLootGeneration;
        buffsChestOpening = data.buffsChestOpening;
        labelIndex = data.labelIndex;
        colorRGBA = data.colorRGBA;
    }

    public static GameObject Spawn(ArmorData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<ArmorBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(ArmorData data, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, parent);
        obj.GetComponent<ArmorBehaviour>().Load(data);
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
        ownerFaction = data.ownerFaction;
        descriptionTextLinkID = data.descriptionTextLinkID;
        inventoryIconLink = data.inventoryIconLink;
        value = data.value;
        pickable = data.pickable;
        removeOnPick = data.removeOnPick;
    }

    public ArmorSlot.Slot slot;
    public float hpIncrease;
    public float speedMultiplierBonus;
    public bool buffsScrapGeneration;
    public bool buffsLootGeneration;
    public bool buffsChestOpening;
    public int labelIndex;
    public string colorRGBA;

}