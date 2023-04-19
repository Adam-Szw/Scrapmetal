using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TriggerBehaviour : MonoBehaviour, Saveable<TriggerData>
{
    public int id = 0;

    public int popupID = 0;   // No popup will be shown if ID is 0

    private bool triggered = false;

    // Player entered a cell
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore collisions unless its with the player
        Transform parent = other.gameObject.transform.parent;
        if (!parent) return;
        PlayerBehaviour b = parent.gameObject.GetComponent<PlayerBehaviour>();
        if (!b) return;
        if (!triggered) TriggerEffects();
    }

    private void TriggerEffects()
    {
        triggered = true;
        // Show popup if assigned
        UIControl.ShowPopup(popupID, 0.2f);
    }

    public TriggerData Save()
    {
        TriggerData data = new TriggerData();
        data.triggered = triggered;
        data.id = id;
        return data;
    }

    public void Load(TriggerData data, bool loadTransform = true)
    {
        triggered = data.triggered;
    }
}

[Serializable]
public class TriggerData
{
    public TriggerData() { }

    public bool triggered;
    public int id;
}