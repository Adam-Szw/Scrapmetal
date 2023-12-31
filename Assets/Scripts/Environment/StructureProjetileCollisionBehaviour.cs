using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class responsible for handling collisions of projectiles and walls
public class StructureProjetileCollisionBehaviour : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // We are only interested in projectiles here
        if (other.gameObject.layer != 10) return;
        ProjectileTrigger t = other.gameObject.GetComponent<ProjectileTrigger>();
        if (!t) return;
        ProjectileBehaviour b = t.behaviour;
        b.RunEffect(b, null);
    }
}
