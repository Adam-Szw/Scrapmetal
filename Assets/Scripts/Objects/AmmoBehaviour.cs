using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AmmoBehaviour;

public class AmmoBehaviour : ItemBehaviour, Saveable<AmmoData>, Spawnable<AmmoData>
{

    public int quantity = 1;
    public AmmoLink link = AmmoLink.empty;

    public enum AmmoLink
    {
        empty, rivet, capacitor, rocket, grenade, dart 
    }

    new public AmmoData Save()
    {
        AmmoData data = new AmmoData(base.Save());
        data.quantity = quantity;
        data.link = link;
        return data;
    }

    public void Load(AmmoData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        quantity = data.quantity;
        link = data.link;
    }

    public static GameObject Spawn(AmmoData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<ItemBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(AmmoData data, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, parent);
        obj.GetComponent<ItemBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class AmmoData : ItemData
{
    public AmmoData() { }

    public AmmoData(ItemData data) : base(data)
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

    public int quantity;
    public AmmoLink link;

}
