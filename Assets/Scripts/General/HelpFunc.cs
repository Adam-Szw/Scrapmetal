using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/* This class contains some useful tools not built-in Unity
 */
class HelpFunc
{
    // Recursively search object and its children for an object by name
    public static GameObject RecursiveFindChild(GameObject parent, string name)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children) if (child.gameObject.name == name) return child.gameObject;
        return null;
    }

    // Search all gameobjects to find one with EntityBehaviour with given ID
    public static GameObject FindGameObjectByBehaviourID(ulong ID)
    {
        List<GameObject> objects = SceneManager.GetActiveScene().GetRootGameObjects().ToList();
        foreach (GameObject o in objects)
        {
            EntityBehaviour behaviour = o.GetComponent<EntityBehaviour>();
            if (behaviour)
            {
                if (behaviour.ID == ID) return o;
            }
        }
        return null;
    }

    public static GameObject FindPlayerInScene()
    {
        List<GameObject> objects = SceneManager.GetActiveScene().GetRootGameObjects().ToList();
        foreach (GameObject o in objects)
        {
            PlayerBehaviour behaviour = o.GetComponent<PlayerBehaviour>();
            if (behaviour)
            {
                return o;
            }
        }
        return null;
    }

    public static float[] VectorToArray(Vector3 vec)
    {
        return new float[3] { vec.x, vec.y, vec.z };
    }

    public static float[] VectorToArray(Vector2 vec)
    {
        return new float[2] { vec.x, vec.y };
    }

    public static List<float[]> VectorListToArrayList(List<Vector2> vl)
    {
        List<float[]> ret = new List<float[]>();
        foreach(Vector2 v in vl)
        {
            ret.Add(VectorToArray(v));
        }
        return ret;
    }

    public static float[] QuaternionToArray(Quaternion q)
    {
        return new float[4] { q.x, q.y, q.z, q.w };
    }

    public static Vector3 DataToVec3(float[] data)
    {
        return new Vector3(data[0], data[1], data[2]);
    }

    public static Vector2 DataToVec2(float[] data)
    {
        return new Vector2(data[0], data[1]);
    }

    public static List<Vector2> DataToListVec2(List<float[]> data)
    {
        List<Vector2> d = new List<Vector2>();
        foreach (float[] fa in data)
        {
            d.Add(DataToVec2(fa));
        }
        return d;
    }

    public static Quaternion DataToQuaternion(float[] data)
    {
        return new Quaternion(data[0], data[1], data[2], data[3]);
    }

    public static float Vec2ToAngle(Vector2 vec)
    {
        float angle = Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg;
        angle = (angle + 360) % 360;
        return angle;
    }

    public static float NormalizeAngle(float angle)
    {
        while (angle <= 0) angle += 360;
        while (angle >= 360) angle -= 360;
        angle = (angle + 360) % 360;
        return angle;
    }

    public static float SmallestAngle(float currAngle, float desiredAngle)
    {
        float targetAngle = (desiredAngle - currAngle + 360) % 360;
        if (targetAngle > 180) targetAngle -= 360;
        return targetAngle;
    }

    public static Vector2 EulerToVec2(float z)
    {
        float angle = z * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        return direction;
    }

    public static Vector2 GetPointAtDistance(Vector2 start, Vector2 target, float distance)
    {
        Vector2 directionVector = start - target;
        directionVector.Normalize();
        directionVector *= distance;
        Vector2 point = target + directionVector;
        return point;
    }

    public static bool PositionInRange(Vector2 start, Vector2 target, float range)
    {
        Vector2 directionVector = target - start;
        float distanceCurr = directionVector.magnitude;
        return distanceCurr <= range;
    }

    public static List<GameObject> GetCreaturesInRadius(Vector2 location, float radius)
    {
        List<GameObject> objects = new List<GameObject>();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(location, radius);
        foreach (Collider2D collider in colliders)
        {
            Transform parent = collider.transform.root;
            CreatureBehaviour behaviour = parent.gameObject.GetComponent<CreatureBehaviour>();
            if (behaviour != null && !objects.Contains(parent.gameObject)) objects.Add(parent.gameObject);
        }
        return objects;
    }

}
