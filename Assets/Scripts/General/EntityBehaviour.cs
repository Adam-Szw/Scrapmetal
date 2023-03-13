using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class controls basic information for all dynamic entities in the game
 */
public class EntityBehaviour : MonoBehaviour
{
    private float speed = 0.0f;
    private Vector3 velocityVector = Vector3.zero;
    private Rigidbody2D rb;

    protected bool alive = true;
    protected float health = 100.0f;

    protected void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody2D>();
    }

    public void SetSpeed(float speed)
    {
        this.speed = speed;
        UpdateRigidBody();
    }

    public void SetVelocityVector(Vector3 velocityVector)
    {
        this.velocityVector = velocityVector;
        UpdateRigidBody();
    }

    public void SetRigidbody(Rigidbody2D rb)
    {
        this.rb = rb;
        UpdateRigidBody();
    }

    public float GetSpeed() { return speed; }
    public Vector3 GetVelocityVector() { return velocityVector; }
    public Rigidbody2D GetRigidbody() { return rb; }

    // Update rigid-body with final vector
    public void UpdateRigidBody()
    {
        rb.velocity = velocityVector.normalized * speed;
    }

    protected EntityData Save()
    {
        EntityData data = new EntityData();
        data.id = this.GetInstanceID();
        data.location = HelpFunc.VectorToArray(transform.localPosition);
        data.rotation = HelpFunc.QuaternionToArray(transform.localRotation);
        data.scale = HelpFunc.VectorToArray(transform.localScale);
        data.velocity = HelpFunc.VectorToArray(GetVelocityVector());
        data.speed = speed;
        data.alive = alive;
        data.health = health;
        return data;
    }

    protected void Load(EntityData data)
    {
        transform.localPosition = HelpFunc.DataToVec3(data.location);
        transform.rotation = HelpFunc.DataToQuaternion(data.rotation);
        transform.localScale = HelpFunc.DataToVec3(data.scale);
        SetVelocityVector(HelpFunc.DataToVec3(data.velocity));
        speed = data.speed;
        alive = data.alive;
        health = data.health;
    }

}
