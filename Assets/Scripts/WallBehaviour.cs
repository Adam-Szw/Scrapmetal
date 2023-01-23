using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBehaviour : MonoBehaviour
{

    [SerializeField] public GameObject frontWall;
    [SerializeField] public GameObject backWall;

    // Thickness of the wall in pixels
    [SerializeField] public float thickness;

    [SerializeField] public Side side;

    public enum Side
    {
        up, down, right, left
    }

    void Start()
    {
        // Scale back wall to match front wall
        SpriteRenderer frontSprRenderer = frontWall.GetComponent<SpriteRenderer>();
        SpriteRenderer backSprRenderer = backWall.GetComponent<SpriteRenderer>();
        backSprRenderer.size = frontSprRenderer.size;

        // Move the back wall to create thickness
        Transform backTransform = backWall.GetComponent<Transform>();
        backTransform.localPosition = new Vector3(0.0f, thickness, 0.0f);

        // Create and add collider
        frontWall.AddComponent<PolygonCollider2D>();
        PolygonCollider2D polygonCollider2D = frontWall.GetComponent<PolygonCollider2D>();
        polygonCollider2D.offset = new Vector2(-frontSprRenderer.size.x * 0.5f, -frontSprRenderer.size.y * 0.5f); // Bottom left as start position
        if (side == Side.up || side == Side.down)
        {
            // Rectangle has easy positions
            polygonCollider2D.points = new Vector2[]
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, thickness),
                new Vector2(frontSprRenderer.size.x, thickness),
                new Vector2(frontSprRenderer.size.x, 0.0f)
            };
        }
        if (side == Side.left || side == Side.right)
        {
            // More tricky to get positions on isometric tile. this is some maths to get it right
            // Angle of the vertical walls is calculated with right triangle of sides 0.35 and 1
            Vector2 cutoffPoint;
            float cutoffYMax = frontSprRenderer.size.x * 0.35f;
            if (cutoffYMax <= frontSprRenderer.size.y) cutoffPoint = new Vector2(0.0f, cutoffYMax);
            else cutoffPoint = new Vector2(frontSprRenderer.size.y / 0.35f, frontSprRenderer.size.y);

            polygonCollider2D.points = new Vector2[]
            {
                new Vector2(cutoffPoint.x, cutoffPoint.y),
                new Vector2(cutoffPoint.x, cutoffPoint.y + thickness),
                new Vector2(frontSprRenderer.size.x, thickness),
                new Vector2(frontSprRenderer.size.x, 0.0f)
            };
        }
    }

    void Update()
    {
        // Set visibility
    }

}
