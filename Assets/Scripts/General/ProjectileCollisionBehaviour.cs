using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileCollisionBehaviour : MonoBehaviour
{

    [SerializeField] public GameObject parent = null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // delegate collision to behaviour script
        //if (!parent) return;
        //if (parent.GetComponent<HumanoidBehaviour>()) 
        //    parent.GetComponent<HumanoidBehaviour>().OnCollision(other);
    }

}
