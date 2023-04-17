using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AreaCell : MonoBehaviour, Saveable<CellData>
{
    public Collider2D collider2d;
    public bool disableByDefault = false;
    public int id;

    public ContentGenerator content = null;

    [HideInInspector] public bool isActive = false;
    [HideInInspector] public bool initialized = false;

    private List<GameObject> held = new List<GameObject>();
    private bool triggered = false;     // True if this cell was already passed through by player and triggered its effects

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
        isActive = true;
        // If first time entering, spawn randomly generated content
        if (!triggered && content) content.Trigger();
        triggered = true;
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
        isActive = false;
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
    }

    public void DeactivateEntitiesInCell()
    {
        List<GameObject> toDeactivate = HelpFunc.GetEntitiesInCollider(collider2d);
        foreach (GameObject obj in toDeactivate)
        {
            held.Add(obj);
            obj.SetActive(false);
        }
    }

    private IEnumerator InitRoutine()
    {
        // Wait 2 frames for objects to settle in and call their start()
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        if (disableByDefault) DeactivateEntitiesInCell();
        initialized = true;
        // Re-trigger entering
        collider2d.enabled = false;
        collider2d.enabled = true;
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