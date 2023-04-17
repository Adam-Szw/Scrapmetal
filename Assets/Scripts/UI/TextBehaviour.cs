using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This marker exists only to help find and remove texts on loading
public class TextBehaviour : MonoBehaviour 
{
    public void Initiate(float time)
    {
        StartCoroutine(UpdateRoutine(time));
    }

    private IEnumerator UpdateRoutine(float time)
    {
        float i = 0;
        while (i < time)
        {
            gameObject.transform.position = gameObject.transform.position + new Vector3(0f, 0.03f, 0f);
            i += 0.05f;
            yield return new WaitForSeconds(.05f);
        }
        Destroy(gameObject);
    }

}
