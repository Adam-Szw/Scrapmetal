using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{

    [SerializeField] public float speedInitial;
    [SerializeField] public float acceleration;
    [SerializeField] public float lifespan;

    public float damage = 0.0f;
    public GameObject owner;

    private Rigidbody2D rb;
    private float lifeRemaining;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lifeRemaining = lifespan;
    }

    void Update()
    {
        rb.velocity = rb.velocity.normalized * Mathf.Max((rb.velocity.magnitude + acceleration * Time.deltaTime), 0.0f);
        lifeRemaining -= Time.deltaTime;
        if (lifeRemaining < 0.0f) Destroy(gameObject);
    }

}
