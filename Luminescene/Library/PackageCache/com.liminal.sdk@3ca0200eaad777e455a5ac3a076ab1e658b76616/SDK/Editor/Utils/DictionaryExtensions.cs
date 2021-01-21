using System.Collections.Generic;

public static class DictionaryExtensions
{
    // Add only if key is not in lookup
    public static void AddSafe<T, TGet>(this Dictionary<T, TGet> lookUp, T key, TGet value)
    {
        if (!lookUp.ContainsKey(key))
            lookUp.Add(key, value);
    }
}