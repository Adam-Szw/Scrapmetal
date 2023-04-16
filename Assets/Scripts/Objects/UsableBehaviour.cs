using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor;
using UnityEngine;


public class UsableBehaviour : ItemBehaviour, Saveable<UsableData>, Spawnable<UsableData>
{

    // How much health will be restored with this item
    public float restoration = 0f;

    new public UsableData Save()
    {
        UsableData data = new UsableData(base.Save());
        data.restoration = restoration;
        return data;
    }

    public void Load(UsableData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        restoration = data.restoration;
    }

    public static GameObject Spawn(UsableData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<UsableBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(UsableData data, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, parent);
        obj.GetComponent<UsableBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class UsableData : ItemData
{
    public UsableData() { }

    public UsableData(ItemData data) : base(data)
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

    public float restoration;

}