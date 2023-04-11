using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Transform = UnityEngine.Transform;

/* This class controls basic information for all dynamic entities in the game
 */
public class EntityBehaviour : MonoBehaviour, Saveable<EntityData>, Spawnable<EntityData>
{
    public string prefabPath;

    [HideInInspector] public ulong ID = 0;
    private float speed = 0.0f;
    private Vector2 moveVector = Vector2.zero;
    private Rigidbody2D rb;

    protected void Update()
    {
        if (GlobalControl.paused) return;
        UpdateRigidBody();
    }

    protected void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody2D>();
        ID = ++GlobalControl.nextID;
        HelpFunc.DisableInternalCollision(transform);
    }

    public void SetSpeed(float speed)
    {
        this.speed = speed;
        UpdateRigidBody();
    }

    public float GetSpeed() { return speed; }

    public void SetMoveVector(Vector2 velocityVector)
    {
        this.moveVector = velocityVector.normalized;
        UpdateRigidBody();
    }

    public Vector2 GetMoveVector() { return moveVector; }

    public void SetRigidbody(Rigidbody2D rb)
    {
        this.rb = rb;
        UpdateRigidBody();
    }

    public Rigidbody2D GetRigidbody() { return rb; }

    // Update rigid-body with final vector
    public void UpdateRigidBody()
    {
        if (!rb) return;
        rb.velocity = moveVector * speed;
    }

    public static GameObject Spawn(string prefabPath, Vector2 position, Quaternion rotation, Transform parent)
    {
        GameObject obj = Instantiate(Resources.Load<GameObject>(prefabPath), position, rotation, parent);
        return obj;
    }

    public static GameObject Spawn(string prefabPath, Vector2 position, Quaternion rotation)
    {
        GameObject obj = Instantiate(Resources.Load<GameObject>(prefabPath), position, rotation);
        return obj;
    }

    public EntityData Save()
    {
        EntityData data = new EntityData();
        data.ID = ID;
        data.prefabPath = prefabPath;
        data.location = HelpFunc.VectorToArray(transform.localPosition);
        data.rotation = HelpFunc.QuaternionToArray(transform.localRotation);
        data.scale = HelpFunc.VectorToArray(transform.localScale);
        data.velocity = HelpFunc.VectorToArray(GetMoveVector());
        data.speed = speed;
        return data;
    }

    public void Load(EntityData data, bool loadTransform = true)
    {
        prefabPath = data.prefabPath;
        if(loadTransform)
        {
            transform.localPosition = HelpFunc.DataToVec3(data.location);
            transform.rotation = HelpFunc.DataToQuaternion(data.rotation);
            transform.localScale = HelpFunc.DataToVec3(data.scale);
        }
        SetMoveVector(HelpFunc.DataToVec2(data.velocity));
        ID = data.ID;
        speed = data.speed;
    }

    public static GameObject Spawn(EntityData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj;
        if (parent != null) obj = Instantiate(Resources.Load<GameObject>(data.prefabPath), position, rotation, parent);
        else obj = Instantiate(Resources.Load<GameObject>(data.prefabPath), position, rotation);
        obj.GetComponent<EntityBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(EntityData data, Transform parent = null)
    {
        GameObject obj;
        if (parent != null) obj = Instantiate(Resources.Load<GameObject>(data.prefabPath), parent);
        else obj = Instantiate(Resources.Load<GameObject>(data.prefabPath));
        obj.GetComponent<EntityBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class EntityData
{
    // Basic information
    public ulong ID;
    public string prefabPath;
    public float[] location;
    public float[] rotation;
    public float[] scale;

    // Entity movement data
    public float[] velocity;
    public float speed;
}