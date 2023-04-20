using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/* Simple trigger script that detects entities entering building obscured areas and redirects events to StructureBehaviour
 */
public class StructureVisibilityAreaTrigger : MonoBehaviour
{

    public StructureBehaviour structure;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerBehaviour b = other.gameObject.GetComponent<PlayerBehaviour>();
        if (b) structure.EnableMasks();
        if (!b) structure.SetHideObject(other.gameObject, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerBehaviour b = other.gameObject.GetComponent<PlayerBehaviour>();
        if (b) structure.DisableMasks();
        structure.SetHideObject(other.gameObject, false);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        PlayerBehaviour b = other.gameObject.GetComponent<PlayerBehaviour>();
        if (b) structure.currPlayerPos = other.gameObject.transform.position;
        // Uncover objects that will be hidden through y-sorting anyway
        if (other.gameObject.transform.position.y > structure.gameObject.transform.parent.position.y) structure.SetHideObject(other.gameObject, false);
    }
}
