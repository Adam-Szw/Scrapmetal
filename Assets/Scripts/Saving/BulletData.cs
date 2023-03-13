using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/* Saved data for bullets
 */
[Serializable]
public class BulletData : EntityData
{
    public BulletData(EntityData data)
    {
        this.id = data.id;
        this.location = data.location;
        this.rotation = data.rotation;
        this.scale = data.scale;
        this.velocity = data.velocity;
        this.speed = data.speed;
        this.alive = data.alive;
        this.health = data.health;
    }

    // Bullet data
    public float speedInitial;
    public float acceleration;
    public float lifespan;
    public float damage;
    public int ownerID;
    public float lifeRemaining;
}