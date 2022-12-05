using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneSceneMonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T s_singleton;
    public static T Instance
    {
        get
        {
            if (s_singleton == null)
            {
                AssignSingleton(FindObjectOfType<T>());
            }
            return s_singleton;
        }
    }

    private static void AssignSingleton(T instance)
    {
        s_singleton = instance;
    }

    protected virtual void Awake()
    {
        if (s_singleton == null)
        {
            AssignSingleton((T)(MonoBehaviour)this);
        }
        else if (s_singleton != this)
        {
            Destroy(gameObject);
        }
    }
}