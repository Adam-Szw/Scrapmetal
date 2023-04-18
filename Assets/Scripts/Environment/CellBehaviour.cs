using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CellBehaviour : MonoBehaviour, Saveable<CellData>
{
    public Collider2D collider2d = null;            // Collider might be needed before Awake so set it up manually
    public bool disableEntitiesByDefault = false;   // If true - everything inside collider is meant to be disabled on start/load
    public int id = 0;                              // We cant generate IDs for those since they are meant to be placed manually in the scene

    public ContentGenerator content = null;         // Content generator that will generate.. well content when cell is activated

    [HideInInspector] public bool isHoldingEntities = false;     // If true it means that the cell disabled entities and is holding onto them
    [HideInInspector] public bool initialized = false;

    private List<GameObject> held = new List<GameObject>();
    private bool triggered = false;                             // True if this cell was already passed through by player and triggered its effects

    private void Start()
    {
        Initialize();
    }

    // Player entered a cell
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Wait for initialization
        if (!initialized) return;
        // Ignore collisions unless its with the player
        Transform parent = other.gameObject.transform.parent;
        if (!parent) return;
        PlayerBehaviour b = parent.gameObject.GetComponent<PlayerBehaviour>();
        if (!b) return;
        ActivateEntitiesInCell();
        isHoldingEntities = true;
        if (!triggered) TriggerEffects();
    }


    // Player left a cell
    private void OnTriggerExit2D(Collider2D other)
    {
        // Wait for initialization
        if (!initialized) return;
        // Ignore collisions unless its with the player
        Transform parent = other.gameObject.transform.parent;
        if (!parent) return;
        PlayerBehaviour b = parent.gameObject.GetComponent<PlayerBehaviour>();
        if (!b) return;
        DeactivateEntitiesInCell();
        isHoldingEntities = false;
    }

    public void Initialize()
    {
        StartCoroutine(InitRoutine());
    }


    public void ActivateEntitiesInCell()
    {
        foreach (GameObject obj in held)
        {
            if (obj)
            {
                obj.SetActive(true);
            }
        }
        held.Clear();
        // Show enabled entities
        StructureBehaviour.UpdateStructures();
    }

    public void DeactivateEntitiesInCell()
    {
        if (collider2d) foreach (GameObject obj in HelpFunc.GetEntitiesInCollider(collider2d))
        {
            held.Add(obj);
            obj.SetActive(false);
        }
    }

    private void TriggerEffects()
    {
        triggered = true;
        // Spawn randomly generated content
        if (content) content.Trigger();
    }

    private IEnumerator InitRoutine()
    {
        // Wait 2 frames for objects to settle in and call their start()
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        if (disableEntitiesByDefault) DeactivateEntitiesInCell();
        initialized = true;
        // Re-trigger entering
        if (collider2d) collider2d.enabled = false;
        if (collider2d) collider2d.enabled = true;
    }

    public CellData Save()
    {
        CellData data = new CellData();
        data.triggered = triggered;
        data.id = id;
        return data;
    }

    public void Load(CellData data, bool loadTransform = true)
    {
        triggered = data.triggered;
    }
}

[Serializable]
public class CellData
{
    public CellData() { }

    public bool triggered;
    public int id;
}