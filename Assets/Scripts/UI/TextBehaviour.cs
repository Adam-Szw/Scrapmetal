using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This marker exists only to help find and remove texts on loading
public class TextBehaviour : MonoBehaviour 
{

    public GameObject follow = null;

    private Vector3 offset;

    public void Initiate(float time, GameObject objectToFollow)
    {
        if (objectToFollow)
        {
            follow = objectToFollow;
            offset = transform.position - follow.transform.position;
        }
        else
        {
            follow = gameObject;
            offset = Vector3.zero;
        }
        StartCoroutine(UpdateRoutine(time));
    }

    private IEnumerator UpdateRoutine(float time)
    {
        float counter = 0;
        while (counter < time)
        {
            offset += new Vector3(0f, 0.015f * (time - counter) / time, 0f);
            Vector3 newPos = follow ? follow.transform.position + offset : transform.position + offset;
            gameObject.transform.position = newPos;
            counter += 0.01f;
            yield return new WaitForSeconds(.01f);
        }
        Destroy(gameObject);
    }

}
