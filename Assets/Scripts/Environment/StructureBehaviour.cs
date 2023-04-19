using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
    public Collider2D structureBoundary;
    public Collider2D hideAreaBoundary;

    [HideInInspector] public Vector2 currPlayerPos;

    private List<GameObject> masks = new List<GameObject>();
    private bool maskEnabled = false;

    public void Start()
    {
        // Create masks
        foreach (GameObject tilemap in tilemapsOutside) CreateMask(tilemap);
        foreach (GameObject tilemap in tilemapsInside) CreateMask(tilemap);
        CreateMask(tilemapOutsideGroundFloor);
        UpdateStructureVisibility();
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

    public static void UpdateStructures()
    {
        foreach (StructureBehaviour structure in FindObjectsOfType<StructureBehaviour>()) structure.UpdateStructureVisibility();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Hide object that entered unless player is inside
        List<GameObject> objInside = HelpFunc.GetObjectsInCollider(structureBoundary);
        if (!objInside.Contains(GlobalControl.GetPlayer())) SetHideObject(other.gameObject, true);

        // For player entering
        if (other.gameObject.GetComponent<PlayerBehaviour>()) PlayerEnteredActions();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // For all objects
        SetHideObject(other.gameObject, false);

        // For player exiting
        if (other.gameObject.GetComponent<PlayerBehaviour>()) PlayerLeftActions();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (GlobalControl.paused) return;
        // Only if triggering object is player
        if (!other.gameObject.GetComponent<PlayerBehaviour>()) return;
        currPlayerPos = other.gameObject.transform.position;
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

    // Makes all objects inside and behind structure to be hidden
    private void UpdateStructureVisibility()
    {
        SetHideAllInside(true);
        SetHideAllBehind(true);
        // Since the player might get caught in this, reverse it rather than checking on every step
        if (GlobalControl.GetPlayer() == null) return;
        GlobalControl.GetPlayer().GetComponentInChildren<SortingGroup>().sortingOrder = 0;
        // If the player is currently inside we also want to show other things
        List<GameObject> objInside = HelpFunc.GetObjectsInCollider(structureBoundary);
        if (objInside.Contains(GlobalControl.GetPlayer()))
        {
            SetHideAllInside(false);
        }
    }

    public void SetHideObject(GameObject obj, bool hide)
    {
        SortingGroup sGroup = obj.GetComponentInChildren<SortingGroup>();
        if (sGroup) sGroup.sortingOrder = hide ? -1 : 0;
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

    private void PlayerEnteredActions()
    {
        // Make outside walls invisible with exception of first floor
        foreach (GameObject tilemap in tilemapsOutside) tilemap.GetComponent<TilemapRenderer>().enabled = false;

        // Make ground floor transparent
        tilemapOutsideGroundFloor.GetComponent<Tilemap>().color = new Color(200.0f, 200.0f, 200.0f, 0.25f);

        // Make inside walls visible
        foreach (GameObject tilemap in tilemapsInside) tilemap.GetComponent<TilemapRenderer>().enabled = true;

        // Make all objects inside be in front of walls
        SetHideAllInside(false);
    }

    private void PlayerLeftActions()
    {
        // Make outside walls visible
        foreach (GameObject tilemap in tilemapsOutside) tilemap.GetComponent<TilemapRenderer>().enabled = true;

        // Make ground floor normal
        tilemapOutsideGroundFloor.GetComponent<Tilemap>().color = new Color(255.0f, 255.0f, 255.0f, 255.0f);

        // Make inside walls invisible
        foreach (GameObject tilemap in tilemapsInside) tilemap.GetComponent<TilemapRenderer>().enabled = false;

        // Hide objects inside
        SetHideAllInside(true);
    }

    private void SetHideAllInside(bool hide)
    {
        List<GameObject> objInside = HelpFunc.GetObjectsInCollider(structureBoundary);
        foreach (GameObject obj in objInside) SetHideObject(obj, hide);
    }

    private void SetHideAllBehind(bool hide)
    {
        List<GameObject> objInside = HelpFunc.GetObjectsInCollider(hideAreaBoundary);
        foreach (GameObject obj in objInside) SetHideObject(obj, hide);
    }

}
