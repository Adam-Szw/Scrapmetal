using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detection : MonoBehaviour
{
    public CreatureBehaviour behaviour;

    // Handle collision with detection collider
    public void OnTriggerEnter2D(Collider2D other)
    {
        behaviour.NotifyDetectedEntity(other);
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        behaviour.NotifyDetectedEntityLeft(other);
    }
}
