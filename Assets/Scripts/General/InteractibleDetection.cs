using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractibleDetection : MonoBehaviour
{
    public PlayerBehaviour behaviour;

    // Sent notification that object entered interaction field
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != 11) return;
        InteractibleTrigger trigger = other.gameObject.GetComponent<InteractibleTrigger>();
        if (trigger) behaviour.NotifyDetectedInteractible(trigger.owner);
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer != 11) return;
        InteractibleTrigger trigger = other.gameObject.GetComponent<InteractibleTrigger>();
        if (trigger) behaviour.NotifyDetectedInteractibleLeft(trigger.owner);
    }
}
