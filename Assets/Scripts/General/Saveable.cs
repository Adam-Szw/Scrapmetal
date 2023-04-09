using UnityEngine;

interface Saveable<T>
{
    T Save();

    void Load(T data, bool loadTransform = true);
}