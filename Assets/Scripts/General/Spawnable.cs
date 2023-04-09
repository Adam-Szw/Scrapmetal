using UnityEngine;

interface Spawnable<T>
{
    static GameObject Spawn(T data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null) { return null; }

    static GameObject Spawn(T data, Transform parent = null) { return null; }
}