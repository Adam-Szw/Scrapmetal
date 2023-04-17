using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/* This script controls visibility inside and around buildings - displaying floors or transparency mask where the player is
 */
public class StructureBehaviour : MonoBehaviour
{
    public GameObject tilemapOutsideGroundFloor;
    // All outside tilemaps should go here except first floor as it has special behaviour and is assigned above
    public List<GameObject> tilemapsOutside;
    public List<GameObject> tilemapsInside;
    public Sprite maskSprite;

    [HideInInspector] public Vector2 currPlayerPos;

    private List<GameObject> masks = new List<GameObject>();
    private bool maskEnabled = false;

    public void Start()
    {
        // Create masks
        foreach (GameObject tilemap in tilemapsOutside) CreateMask(tilemap);
        foreach (GameObject tilemap in tilemapsInside) CreateMask(tilemap);
        CreateMask(tilemapOutsideGroundFloor);
    }

    public void Update()
    {
        if (GlobalControl.paused) return;
        // Move all masks so the player is visible
        if (maskEnabled)
        {
            foreach (GameObject mask in masks)
            {
                mask.transform.position = currPlayerPos;
            }
        }
    }

    private void CreateMask(GameObject parent)
    {
        GameObject mask = new GameObject("Mask");
        mask.transform.parent = parent.transform;
        mask.transform.localEulerAngles = new Vector3(0f, 0f, 19.29005f);
        SpriteMask sprMask = mask.AddComponent<SpriteMask>();
        sprMask.sprite = maskSprite;
        sprMask.alphaCutoff = 1f;
        sprMask.enabled = false;
        masks.Add(mask);
    }

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
        if (GlobalControl.paused) return;
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

}
