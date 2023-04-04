using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static WeaponBehaviour;

public class ObjectBehaviour : EntityBehaviour
{
    public string prefabPath;
    public ulong ownerID = 0;

    public virtual void Use() { }

    public new ObjectData Save()
    {
        ObjectData data = new ObjectData(base.Save());
        data.prefabPath = prefabPath;
        data.ownerID = ownerID;
        return data;
    }

    /* dontLoadBody refers to not loading transform and rigibdody data.
     * This is useful when spawning things at a new location using ObjectData
     */
    public void Load(ObjectData data, bool dontLoadBody = false)
    {
        if (!dontLoadBody) base.Load(data);
        this.prefabPath = data.prefabPath;
        this.ownerID = data.ownerID;
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

    public string prefabPath;
    public ulong ownerID;
}