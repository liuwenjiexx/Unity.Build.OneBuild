using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
{
    [SerializeField]
    private TKey[] keys;
    [SerializeField]
    private TValue[] values;

    Dictionary<TKey, TValue> target;
    public Dictionary<TKey, TValue> Dictionary() { return target; }

    public SerializableDictionary(Dictionary<TKey, TValue> target)
    {
        this.target = target;
    }

    public void OnBeforeSerialize()
    {
        keys = target.Keys.ToArray();
        values = target.Values.ToArray();
        
    }

    public void OnAfterDeserialize()
    {
        var count = Math.Min(keys.Length, values.Length);
        target = new Dictionary<TKey, TValue>(count);
        for (var i = 0; i < count; ++i)
        {
            target.Add(keys[i], values[i]);
        }
        keys = null;
        values = null;
    }
}
