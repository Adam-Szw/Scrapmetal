using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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

    public static float[] VectorToArray(Vector3 vec)
    {
        return new float[3] { vec.x, vec.y, vec.z };
    }

    public static float[] VectorToArray(Vector2 vec)
    {
        return new float[2] { vec.x, vec.y };
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

    public static Quaternion DataToQuaternion(float[] data)
    {
        return new Quaternion(data[0], data[1], data[2], data[3]);
    }

}
