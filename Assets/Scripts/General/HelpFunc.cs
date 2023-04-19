using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

/* This class contains some useful tools not built-in Unity
 */
public static class HelpFunc
{
    // Recursively search object and its children for an object by name
    public static GameObject RecursiveFindChild(GameObject parent, string name)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children) if (child.gameObject.name == name) return child.gameObject;
        return null;
    }

    // Search all gameobjects to find one with EntityBehaviour with given ID
    public static GameObject FindEntityByID(ulong ID)
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

    // Searches for objects with CreatureBehaviour using hitbox layer
    public static List<CreatureBehaviour> GetCreaturesInRadiusByHitbox(Vector2 position, float radius)
    {
        List<CreatureBehaviour> creatures = new List<CreatureBehaviour>();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius, 1 << 7);
        foreach (Collider2D collider in colliders)
        {
            CreatureBehaviour b = collider.GetComponentInParent<CreatureBehaviour>();
            if (b != null && !creatures.Contains(b)) creatures.Add(b);
        }
        return creatures;
    }

    public static List<GameObject> GetEntitiesInCollider(Collider2D collider)
    {
        List<GameObject> objects = GetObjectsInCollider(collider);
        List<GameObject> creatures = new List<GameObject>();
        foreach (GameObject obj in objects)
        {
            EntityBehaviour b = obj.GetComponentInParent<EntityBehaviour>();
            if (b != null && (b is not PlayerBehaviour) && !creatures.Contains(b.gameObject)) creatures.Add(b.gameObject);
        }
        return creatures;
    }

    public static List<GameObject> GetObjectsInCollider(Collider2D collider)
    {
        List<GameObject> objects = new List<GameObject>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.layerMask = 1 << 0;
        int resultCount = 0;
        Collider2D[] colliders = new Collider2D[999];
        resultCount = Physics2D.OverlapCollider(collider, filter, colliders);
        for (int i = 0; i < resultCount; i++) objects.Add(colliders[i].gameObject);
        return objects;
    }

    // Finds all cells in currently active scene
    public static Dictionary<int, CellBehaviour> GetCells()
    {
        CellBehaviour[] cells = MonoBehaviour.FindObjectsOfType<CellBehaviour>();
        Dictionary<int, CellBehaviour> cellsDict = new Dictionary<int, CellBehaviour>();
        for (int i = 0; i < cells.Length; i++) cellsDict[cells[i].id] = cells[i];
        return cellsDict;
    }

    // Finds all triggers in currently active scene
    public static Dictionary<int, TriggerBehaviour> GetTriggers()
    {
        TriggerBehaviour[] triggers = MonoBehaviour.FindObjectsOfType<TriggerBehaviour>();
        Dictionary<int, TriggerBehaviour> triggersDict = new Dictionary<int, TriggerBehaviour>();
        for (int i = 0; i < triggers.Length; i++) triggersDict[triggers[i].id] = triggers[i];
        return triggersDict;
    }


    public static List<GameObject> GetAllObjectsInScene()
    {
        List<GameObject> objects = new List<GameObject>();
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects)
        {
            GetChildrenIntoListRecursive(objects, rootObject);
        }
        return objects;
    }

    public static void GetChildrenIntoListRecursive(List<GameObject> list, GameObject parent)
    {
        if (!list.Contains(parent)) list.Add(parent);
        foreach (Transform child in parent.transform) GetChildrenIntoListRecursive(list, child.gameObject);
    }

    public static List<GameObject> GetAllChildObjects(GameObject parentObject)
    {
        List<GameObject> childObjects = new List<GameObject>();
        foreach (Transform childTransform in parentObject.transform)
        {
            GameObject childObject = childTransform.gameObject;
            childObjects.Add(childObject);
            childObjects.AddRange(GetAllChildObjects(childObject));
        }
        return childObjects;
    }

    // Recursively disables all colliders in the object
    public static void DisableColliders(Transform parent)
    {
        Collider2D[] colliders = parent.gameObject.GetComponentsInParent<Collider2D>();
        foreach (Collider2D collider in colliders) collider.enabled = false;
        Collider2D[] childColliders = parent.gameObject.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in childColliders) collider.enabled = false;
    }

    public static void DisableInternalCollision(Transform parent)
    {
        List<Collider2D> colliders = new List<Collider2D>(parent.gameObject.GetComponentsInChildren<Collider2D>());
        Collider2D pCollider = parent.gameObject.GetComponent<Collider2D>();
        if (pCollider) colliders.Add(pCollider);

        foreach (Collider2D collider in colliders)
        {
            foreach (Collider2D otherCollider in colliders)
            {
                if (collider != otherCollider)
                {
                    Physics2D.IgnoreCollision(collider, otherCollider);
                }
            }
        }
    }

    public static Vector2 GetRandomPointInPolygonCollider(PolygonCollider2D collider)
    {
        Vector2 randomPoint = Vector2.zero;
        Bounds bounds = collider.bounds;
        float minX = bounds.min.x;
        float maxX = bounds.max.x;
        float minY = bounds.min.y;
        float maxY = bounds.max.y;
        bool pointFound = false;
        while (!pointFound)
        {
            randomPoint = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            if (collider.OverlapPoint(randomPoint))
            {
                pointFound = true;
            }
        }
        return randomPoint;
    }

    public static T DeepCopy<T>(T source)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream stream = new MemoryStream();
        formatter.Serialize(stream, source);
        stream.Seek(0, SeekOrigin.Begin);
        T copy = (T)formatter.Deserialize(stream);
        stream.Close();
        return copy;
    }
}
