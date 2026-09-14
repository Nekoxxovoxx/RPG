using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Stat
{
    [SerializeField, InspectorName("基础数值")] private int baseValue;

    [InspectorName("加成列表")]
    public List<int> modifiers;

    public int BaseValue => baseValue;

    public int GetValue()
    {
        int finalValue = baseValue;

        if (modifiers == null)
            modifiers = new List<int>();

        foreach (int modifier in modifiers)
            finalValue += modifier;

        return finalValue;
    }

    public void SetDefaultValue(int _value)
    {
        baseValue = _value;
    }

    public void AddBaseValue(int _value)
    {
        baseValue += _value;
    }

    public void AddModifier(int _modifier)
    {
        if (modifiers == null)
            modifiers = new List<int>();

        modifiers.Add(_modifier);
    }

    public void RemoveModifier(int _modifier)
    {
        if (modifiers == null)
            return;

        modifiers.Remove(_modifier);
    }
}
