using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.FilePathAttribute;
using UnityEngine.UIElements;

/* This class controls basic information for all dynamic entities in the game
 */
public class EntityBehaviour : MonoBehaviour
{
    private float speed = 0.0f;
    private Vector3 velocityVector = Vector3.zero;
    private Rigidbody2D rb;

    protected void Update()
    {
        if (GlobalControl.paused) return;
        UpdateRigidBody();
    }

    protected void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody2D>();
    }

    public void SetSpeed(float speed)
    {
        this.speed = speed;
        UpdateRigidBody();
    }

    public float GetSpeed() { return speed; }

    public void SetVelocity(Vector3 velocityVector)
    {
        this.velocityVector = velocityVector;
        UpdateRigidBody();
    }

    public Vector3 GetVelocity() { return velocityVector; }

    public void SetRigidbody(Rigidbody2D rb)
    {
        this.rb = rb;
        UpdateRigidBody();
    }

    public Rigidbody2D GetRigidbody() { return rb; }

    // Update rigid-body with final vector
    public void UpdateRigidBody()
    {
        rb.velocity = velocityVector.normalized * speed;
    }


    // Recursively disables all colliders in the object
    protected void DisableColliders(Transform parent)
    {
        CapsuleCollider2D capsuleCollider = parent.GetComponent<CapsuleCollider2D>();
        if (capsuleCollider != null) capsuleCollider.enabled = false;
        BoxCollider2D boxCollider = parent.GetComponent<BoxCollider2D>();
        if (boxCollider != null) boxCollider.enabled = false;
        foreach (Transform child in parent)
        {
            capsuleCollider = child.GetComponent<CapsuleCollider2D>();
            if (capsuleCollider != null) capsuleCollider.enabled = false;
            boxCollider = child.GetComponent<BoxCollider2D>();
            if (boxCollider != null) boxCollider.enabled = false;
            DisableColliders(child);
        }
    }

    protected EntityData Save()
    {
        EntityData data = new EntityData();
        data.id = this.GetInstanceID();
        data.location = HelpFunc.VectorToArray(transform.localPosition);
        data.rotation = HelpFunc.QuaternionToArray(transform.localRotation);
        data.scale = HelpFunc.VectorToArray(transform.localScale);
        data.velocity = HelpFunc.VectorToArray(GetVelocity());
        data.speed = this.speed;
        return data;
    }

    protected void Load(EntityData data)
    {
        transform.localPosition = HelpFunc.DataToVec3(data.location);
        transform.rotation = HelpFunc.DataToQuaternion(data.rotation);
        transform.localScale = HelpFunc.DataToVec3(data.scale);
        SetVelocity(HelpFunc.DataToVec3(data.velocity));
        speed = data.speed;
    }

}

[Serializable]
public class EntityData
{
    // Basic information
    public int id;
    public float[] location;
    public float[] rotation;
    public float[] scale;

    // Entity movement data
    public float[] velocity;
    public float speed;
}