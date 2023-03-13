using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : EntityBehaviour
{
    public float speedInitial;
    public float acceleration;
    public float lifespan;
    public float lifeRemaining;
    public float damage;
    public int ownerID;

    void Update()
    {
        if (GlobalControl.paused) return;
        SetSpeed(Mathf.Max(GetSpeed() + acceleration * Time.deltaTime, 0.0f));
        UpdateRigidBody();
        lifeRemaining -= Time.deltaTime;
        if (lifeRemaining < 0.0f) Destroy(gameObject);
    }

    /* Get all information about this bullet for saving
     */
    new public BulletData Save()
    {
        BulletData data = new BulletData(base.Save());
        data.speedInitial = speedInitial;
        data.acceleration = acceleration;
        data.lifespan = lifespan;
        data.damage = damage;
        data.ownerID = ownerID;
        data.lifeRemaining = lifeRemaining;
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

}
