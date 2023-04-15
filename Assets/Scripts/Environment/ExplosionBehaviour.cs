using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionBehaviour : MonoBehaviour
{
    public float lifespan = 0.1f;

    void Start()
    {
        StartCoroutine(SelfDestruct(lifespan));
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private IEnumerator SelfDestruct(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }

}
