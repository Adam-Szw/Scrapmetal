using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : ItemBehaviour
{
    public float speedInitial;
    public float acceleration;
    public float lifespan;
    public float lifeRemaining;
    public float damage;

    new protected void Update()
    {
        base.Update();
        SetSpeed(Mathf.Max(GetSpeed() + acceleration * Time.deltaTime, 0.0f));
        UpdateRigidBody();
        lifeRemaining -= Time.deltaTime;
        if (lifeRemaining < 0.0f) Destroy(gameObject);
    }

    /* Get all information about this bullet for saving
     */
    new public BulletData Save()
    {
        BulletData data = new BulletData(base.Save())
        {
            speedInitial = speedInitial,
            acceleration = acceleration,
            lifespan = lifespan,
            damage = damage,
            ownerID = ownerID,
            lifeRemaining = lifeRemaining
        };
        return data;
    }

    public void Load(BulletData data)
    {
        base.Load(data);
        speedInitial = data.speedInitial;
        acceleration = data.acceleration;
        lifespan = data.lifespan;
        damage = data.damage;
        ownerID = data.ownerID;
        lifeRemaining = data.lifeRemaining;
    }

    public static void SpawnEntity(BulletData data)
    {
        GameObject obj = Instantiate(Resources.Load<GameObject>(BulletBehaviour.PREFAB_PATH));
        obj.GetComponent<BulletBehaviour>().Load(data);
    }

}

[Serializable]
public class BulletData : EntityData
{
    public BulletData() { }

    public BulletData(EntityData data)
    {
        this.id = data.id;
        this.location = data.location;
        this.rotation = data.rotation;
        this.scale = data.scale;
        this.velocity = data.velocity;
        this.speed = data.speed;
    }

    // Bullet data
    public float speedInitial;
    public float acceleration;
    public float lifespan;
    public float damage;
    public int ownerID;
    public float lifeRemaining;
}