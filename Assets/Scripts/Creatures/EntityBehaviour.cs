using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Antlr3.Runtime.Tree.TreeWizard;
using Transform = UnityEngine.Transform;

/* This class controls basic information for all dynamic entities in the game
 */
public class EntityBehaviour : MonoBehaviour, Saveable<EntityData>, Spawnable<EntityData>
{
    public delegate void InteractionEffect(CreatureBehaviour user);

    public string prefabPath;                       // Path to the prefab for this entity. Used frequently when spawning things
    public GameObject targetBone = null;            // Object at which enemy weapons will be targeted
    public GameObject interactAttachment = null;    // Interaction and floating texts will reference position of this object

    [HideInInspector] public ulong ID = 0;
    [HideInInspector] public bool dontSave = false;
    private float speed = 0.0f;                     // This stuff is for rigidbody velocity calculation
    private Vector2 moveVector = Vector2.zero;
    private Rigidbody2D rb = null;
    // Effect to be triggered when this entity enters interaction field
    [HideInInspector] public InteractionEffect interactionEnterEffect = null;
    // Effect to be triggered when this entity is interacted with
    [HideInInspector] public InteractionEffect interactionUseEffect = null;
    // Texts and miscelaneous attached to this object
    protected GameObject aura = null;
    protected GameObject hText = null;
    protected GameObject interactionColliderObj = null;

    protected void Update()
    {
        if (GlobalControl.paused) return;
        UpdateRigidBody();
    }

    protected void Awake()
    {
        // Setup ID and RB
        rb = this.gameObject.GetComponent<Rigidbody2D>();
        ID = ++GlobalControl.nextID;
        HelpFunc.DisableInternalCollision(transform);
    }

    protected void OnDestroy()
    {
        // Destroy texts attached
        if (aura) Destroy(aura);
        if (hText) Destroy(hText);
        StopAllCoroutines();
    }

    // Adds collider for interactible detection by the player, essentially making this object detectable for interactions
    public void AddInteractionCollider()
    {
        interactionColliderObj = new GameObject("InteractionCollider");
        interactionColliderObj.transform.parent = transform;
        interactionColliderObj.transform.localPosition = Vector3.zero;
        interactionColliderObj.layer = 11;
        InteractibleTrigger trigger = interactionColliderObj.AddComponent<InteractibleTrigger>();
        trigger.owner = gameObject;
        CircleCollider2D collider = interactionColliderObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.1f;
    }

    public void RemoveInteractionCollider()
    {
        if (interactionColliderObj != null) Destroy(interactionColliderObj);
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
        if (rb.bodyType != RigidbodyType2D.Static) rb.velocity = moveVector * speed;
    }

    // Spawn a text in interaction area for a given amount of time
    public void SpawnFloatingText(Color color, string text, float time)
    {
        GameObject floatingText = Instantiate(Resources.Load<GameObject>("Prefabs/UI/TextObjectLight"));
        floatingText.GetComponentInChildren<TextMeshProUGUI>().text = text;
        floatingText.GetComponentInChildren<TextMeshProUGUI>().color = color;
        floatingText.transform.position = interactAttachment.transform.position + new Vector3(0f, 0f, 0f);
        floatingText.GetComponent<TextBehaviour>().Initiate(time, gameObject);
    }

    // Coroutine causes the text to float a distance over time
    protected IEnumerator SpawnInteractionTextCoroutine(string text, float time, float yOffset)
    {
        hText = Instantiate(Resources.Load<GameObject>("Prefabs/UI/TextObject"));
        hText.GetComponentInChildren<TextMeshProUGUI>().text = text;
        hText.transform.position = interactAttachment.transform.position + new Vector3(0f, yOffset, 0f);
        yield return new WaitForSeconds(time);
        Destroy(hText);
        hText = null;
    }

    // Envelop entity in aura for given time - used in interactions
    protected IEnumerator HighlightEntityCoroutine(float time)
    {
        aura = Instantiate(Resources.Load<GameObject>("Prefabs/UI/HighlightAura"));
        aura.transform.parent = transform;
        aura.transform.localPosition = Vector3.zero;
        yield return new WaitForSeconds(time);
        Destroy(aura);
        aura = null;
    }

    public EntityData Save()
    {
        EntityData data = new EntityData();
        data.ID = ID;
        data.prefabPath = prefabPath;
        data.location = HelpFunc.VectorToArray(transform.position);
        data.rotation = HelpFunc.QuaternionToArray(transform.rotation);
        data.scale = HelpFunc.VectorToArray(transform.localScale);
        data.velocity = HelpFunc.VectorToArray(GetMoveVector());
        data.speed = speed;
        data.active = gameObject.activeSelf;
        return data;
    }

    public void Load(EntityData data, bool loadTransform = true)
    {
        prefabPath = data.prefabPath;
        if(loadTransform)
        {
            transform.position = HelpFunc.DataToVec3(data.location);
            transform.rotation = HelpFunc.DataToQuaternion(data.rotation);
            transform.localScale = HelpFunc.DataToVec3(data.scale);
        }
        SetMoveVector(HelpFunc.DataToVec2(data.velocity));
        ID = data.ID;
        speed = data.speed;
        gameObject.SetActive(data.active);
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
    public bool active;
    public ulong ID;
    public string prefabPath;
    public float[] location;
    public float[] rotation;
    public float[] scale;

    // Entity movement data
    public float[] velocity;
    public float speed;
}