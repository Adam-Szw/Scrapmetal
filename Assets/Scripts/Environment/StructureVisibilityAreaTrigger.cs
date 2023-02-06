using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureVisibilityAreaTrigger : MonoBehaviour
{

    private StructureBehaviour structureBehaviour;

    private void Start()
    {
        structureBehaviour = transform.parent.gameObject.GetComponent<StructureBehaviour>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<PlayerBehaviour>())
            structureBehaviour.EnableMasks();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<PlayerBehaviour>())
            structureBehaviour.DisableMasks();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.gameObject.GetComponent<PlayerBehaviour>()) return;
        structureBehaviour.currPlayerPos = other.gameObject.transform.position;
    }
}
