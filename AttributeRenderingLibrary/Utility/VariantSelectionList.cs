using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace AttributeRenderingLibrary;

/// <summary>
/// This class is designed to store test-set + value pairs
/// </summary>
/// <typeparam name="TItem">value type</typeparam>
public class VariantSelectionList<TItem>
{
    /// <summary>
    /// Cached pattern data for fast matching
    /// </summary>
    public struct TestPattern
    {
        public bool MatchAny;
        public string Key;
        public string Value;

        /// <summary>
        /// Construct pattern from string
        /// </summary>
        /// <param name="str">string pattern, can be explicit (attr-val) or wildcard (attr-*, *-val, *, etc.)</param>
        /// <param name="outPattern">constructed pattern</param>
        /// <returns>true on success</returns>
        public static bool CreateFromString(string str, out TestPattern outPattern)
        {
            str = str.Trim();
            if (str == "*")
            {
                outPattern = new TestPattern
                {
                    MatchAny = true,
                };
                return true;
            }
            else
            {
                int separatorPos = str.IndexOf('-');
                if (separatorPos == -1)
                {
                    outPattern = default;
                    return false;
                }
                
                string key = str.Substring(0, separatorPos).Trim();
                string value = str.Substring(separatorPos + 1).Trim();

                if (key.Length == 0 || value.Length == 0)
                {
                    outPattern = default;
                    return false;
                }
                
                outPattern = new TestPattern
                {
                    Key = key,
                    Value = value,
                };
                return true;
            }
        }

        /// <summary>
        /// Test this pattern against variants
        /// </summary>
        /// <param name="variants">variants to test against</param>
        /// <returns>true on any match</returns>
        public bool Test(Variants variants)
        {
            if (MatchAny)
                return true;

            bool wildcardKey = Key.Contains("*");
            bool wildcardValue = Value.Contains("*");
            
            bool anyKey = wildcardKey && Key.Length == 1;
            bool anyValue = wildcardValue && Value.Length == 1;

            bool TestValue(string pattern, string value)
            {
                if (anyValue)
                    return true;
                if (wildcardValue)
                    return WildcardUtil.Match(pattern, value);
                return pattern == value;
            }

            if (anyKey)
            {
                foreach (var variant in variants.Elements)
                {
                    if (TestValue(Value, variant.Value))
                        return true;
                }
            }
            if (wildcardKey) // Slowest option, check every entry
            {
                foreach (var variant in variants.Elements)
                {
                    if (WildcardUtil.Match(Key, variant.Key))
                    {
                        if (TestValue(Value, variant.Value))
                            return true;
                    }
                }
            }
            else // Faster option, value lookup
            {
                if (variants.Elements.TryGetValue(Key, out string variantValue))
                {
                    return TestValue(Value, variantValue);
                }
            }

            return false;
        }
    }

    public List<KeyValuePair<TestPattern[], TItem>> Entries { get; private init; } = new();
    public int Count => Entries.Count;
    
    public static VariantSelectionList<TItem> LoadFrom(IDictionary<string, TItem> dict) => LoadFrom(dict, item => item);

    public static VariantSelectionList<TItem> LoadFrom(JObject obj) => LoadFrom(obj, item => item.ToObject<TItem>());
    
    public static VariantSelectionList<TItem> LoadFrom(JsonObject obj) => LoadFrom(obj.Token as JObject);

    public static VariantSelectionList<TItem> LoadFrom<TSourceItem>(IDictionary<string, TSourceItem> dict, Converter<TSourceItem, TItem> converter)
    {
        if (dict is not { Count: > 0 })
            return null;
        
        VariantSelectionList<TItem> result = new();

        foreach (var entry in dict)
        {
            var patternStrings = entry.Key.Split("::", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            List<TestPattern> patterns = new();

            foreach (var patternString in patternStrings)
            {
                if (!TestPattern.CreateFromString(patternString, out var pattern))
                    throw new ArgumentException($"Failed to create test pattern {patternString}");
                
                patterns.Add(pattern);
            }
            
            result.Entries.Add(new KeyValuePair<TestPattern[], TItem>(patterns.ToArray(), converter(entry.Value)));
        }

        return result;
    }
    
    public bool FindFirstByVariant(Variants variants, out TItem outItem)
    {
        foreach (var entry in Entries)
        {
            if (entry.Key.All(pattern => pattern.Test(variants)))
            {
                outItem = entry.Value;
                return true;
            }
        }

        outItem = default;
        return false;
    }
    
    public IEnumerable<TItem> FindAllByVariant(Variants variants)
    {
        foreach (var entry in Entries)
        {
            if (entry.Key.All(pattern => pattern.Test(variants)))
            {
                yield return entry.Value;
            }
        }
    }
}
