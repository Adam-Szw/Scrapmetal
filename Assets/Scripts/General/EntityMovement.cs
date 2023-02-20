using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityMovement : MonoBehaviour
{

    public float speed;
    public Vector3 velocityVector = Vector3.zero;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (GlobalControl.paused) return;

        rb.velocity = velocityVector.normalized * speed;
    }

}
