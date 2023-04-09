using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static WeaponBehaviour;

public class ObjectBehaviour : EntityBehaviour, Saveable<ObjectData>, Spawnable<ObjectData>
{
    public ulong ownerID = 0;

    public virtual void Use() { }

    public new ObjectData Save()
    {
        ObjectData data = new ObjectData(base.Save());
        data.prefabPath = prefabPath;
        data.ownerID = ownerID;
        return data;
    }

    public void Load(ObjectData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        this.ownerID = data.ownerID;
    }

    public static GameObject Spawn(ObjectData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = EntityBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<ObjectBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(ObjectData data, Transform parent = null)
    {
        GameObject obj = EntityBehaviour.Spawn(data, parent);
        obj.GetComponent<ObjectBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class ObjectData : EntityData
{
    public ObjectData() { }

    public ObjectData(EntityData data)
    {
        this.ID = data.ID;
        this.location = data.location;
        this.rotation = data.rotation;
        this.scale = data.scale;
        this.velocity = data.velocity;
        this.speed = data.speed;
    }

    public ulong ownerID;
}