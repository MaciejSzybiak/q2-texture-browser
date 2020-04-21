/**
Copyright (C) 2020 Maciej Szybiak

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see https://www.gnu.org/licenses/.
*/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pool for any class type derived from MonoBehaviour.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    #region unity properties
    [Header("General")]
    [SerializeField]
    private MonoBehaviour Prefab = default;
    [SerializeField]
    private Transform Parent = default;
    [Header("Pool properties")]
    [SerializeField]
    [Min(10)]
    private int InitialCapacity = 100;
    [SerializeField]
    [Min(10)]
    private int MaxCapacity = 2000;
    [SerializeField]
    private bool AutoInitialize = false;
    #endregion

    private List<MonoBehaviour> objects;

    private void Awake()
    {
        if (AutoInitialize)
        {
            Initialize();
        }
    }

    /// <summary>
    /// Initializes the object pool's array and instantiates initial object count.
    /// </summary>
    public void Initialize()
    {
        if (objects != null)
        {
            Debug.LogAssertion("Tried to reinitialize object pool.", this);
            return;
        }

        objects = new List<MonoBehaviour>(InitialCapacity);

        for (int i = 0; i < InitialCapacity; i++)
        {
            objects.Add(GetNew());
            objects[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gets a new object from the pool.
    /// </summary>
    /// <returns>MonoBehaviour instance.</returns>
    public MonoBehaviour Get()
    {
        Debug.Assert(objects != null, "Object list is null", this);

        MonoBehaviour obj;
        int last = objects.Count - 1;

        if (last >= 0)
        {
            obj = objects[last];
            objects.RemoveAt(last);
        }
        else
        {
            obj = GetNew();
        }
        obj.gameObject.SetActive(true);
        obj.transform.SetAsLastSibling();
        return obj;
    }

    /// <summary>
    /// Gets a new object from the pool. Adds a single GetComponent() call.
    /// </summary>
    /// <typeparam name="T">MonoBehaviour derived class.</typeparam>
    /// <returns>MonoBehaviour component of type T.</returns>
    public T Get<T>() where T : MonoBehaviour
    {
        T component = Get().GetComponent<T>();
        Debug.Assert(component, "Incorrect generic type: object has no component of this type.", this);
        return component;
    }

    /// <summary>
    /// Hides object from the world and returns it to the pool.
    /// </summary>
    /// <param name="obj">Object to hide.</param>
    public void Return(MonoBehaviour obj)
    {
        if(objects.Count == MaxCapacity)
        {
            Destroy(obj.gameObject);
        }
        else
        {
            obj.gameObject.SetActive(false);
            objects.Add(obj);
        }
    }

    /// <summary>
    /// Instantiates a new pool object.
    /// </summary>
    /// <returns>New MonoBehaviour instance.</returns>
    private MonoBehaviour GetNew()
    {
        return Instantiate(Prefab, Parent);
    }
}
