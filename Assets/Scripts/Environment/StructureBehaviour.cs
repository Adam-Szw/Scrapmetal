using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StructureBehaviour : MonoBehaviour
{

    public Vector2 currPlayerPos = Vector2.zero;

    [SerializeField] private GameObject tilemapOutsideGroundFloor;
    // All outside tilemaps should go here except first floor as it has special behaviour and is assigned above
    [SerializeField] private List<GameObject> tilemapsOutside;
    [SerializeField] private List<GameObject> tilemapsInside;
    [SerializeField] private List<GameObject> masks;

    private bool maskEnabled = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only if triggering object is player
        if (!other.gameObject.GetComponent<PlayerBehaviour>()) return;

        // Make outside walls invisible with exception of first floor
        foreach (GameObject tilemap in tilemapsOutside)
        {
            tilemap.GetComponent<TilemapRenderer>().enabled = false;
        }

        // Make ground floor transparent
        tilemapOutsideGroundFloor.GetComponent<Tilemap>().color = new Color(200.0f, 200.0f, 200.0f, 0.65f);

        // Make inside walls visible
        foreach (GameObject tilemap in tilemapsInside)
        {
            tilemap.GetComponent<TilemapRenderer>().enabled = true;
        }

    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Only if triggering object is player
        if (!other.gameObject.GetComponent<PlayerBehaviour>()) return;

        // Make outside walls visible
        foreach (GameObject tilemap in tilemapsOutside)
        {
            tilemap.GetComponent<TilemapRenderer>().enabled = true;
        }

        // Make ground floor normal
        tilemapOutsideGroundFloor.GetComponent<Tilemap>().color = new Color(255.0f, 255.0f, 255.0f, 255.0f);

        // Make inside walls invisible
        foreach (GameObject tilemap in tilemapsInside)
        {
            tilemap.GetComponent<TilemapRenderer>().enabled = false;
        }

    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Only if triggering object is player
        if (!other.gameObject.GetComponent<PlayerBehaviour>()) return;
        currPlayerPos = other.gameObject.transform.position;
    }

    public void EnableMasks()
    {
        maskEnabled = true;
        foreach (GameObject mask in masks)
        {
            mask.GetComponent<SpriteMask>().enabled = true;
        }
    }

    public void DisableMasks()
    {
        maskEnabled = false;
        foreach (GameObject mask in masks)
        {
            mask.GetComponent<SpriteMask>().enabled = false;
        }
    }

    void Update()
    {
        if(maskEnabled)
        {
            foreach(GameObject mask in masks)
            {
                mask.transform.position = currPlayerPos;
            }
        }
    }

}
