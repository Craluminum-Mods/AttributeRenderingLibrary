using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace AttributeRenderingLibrary;

public static class VariantExtensions
{
    /// <summary>
    /// Similar to ByType, tries to match key (or multiple keys, if there is '::' separator used as AND operator) and give value behind it
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="variants"></param>
    /// <param name="inDictionary">List of keys, including keys with '::' separator used as AND operator </param>
    /// <param name="result"></param>
    /// <returns>True, if value by key is found, otherwise false</returns>
    public static bool FindByVariant<T>(this Variants variants, Dictionary<string, T> inDictionary, out T result)
    {
        Core.Api?.World?.FrameProfiler?.Enter("AttributeRenderingLibrary.FindByVariant");
        result = default!;

        if (variants == null || variants.Count == 0 || inDictionary is not { Count: > 0 })
        {
            Core.Api?.World?.FrameProfiler?.Leave();
            return false;
        }

        List<string> variantAsStringArray = variants.GetAsStringArray();
        foreach ((string key, T value) in inDictionary)
        {
            string[] keys = key.Contains("::") ? key.Split("::") : [key];
            if (keys.All(k => variantAsStringArray.Any(v => WildcardUtil.Match(k, v))))
            {
                result = value;
                Core.Api?.World?.FrameProfiler?.Leave();
                return true;
            }
        }

        Core.Api?.World?.FrameProfiler?.Leave();
        return false;
    }

    /// <summary>
    /// Alias of FindByVariant for ItemStack, to avoid null check for both dictionary and Variants every time
    /// </summary>
    public static bool FindByVariant<T>(this ItemStack stack, Dictionary<string, T> inDictionary, out T result)
    {
        result = default!;

        return stack != null && inDictionary is { Count: > 0 } && FindByVariant(Variants.FromStack(stack), inDictionary, out result);
    }

    /// <summary>
    /// Alias of FindByVariant for ItemStack, to avoid null check for both dictionary and Variants every time.
    /// <br/> Also returns Variants as out parameter to avoid creating it multiple times if needed for other operations after finding the value.
    /// </summary>
    public static bool FindByVariant<T>(this ItemStack stack, Dictionary<string, T> inDictionary, out T result, out Variants variants)
    {
        result = default!;

        if (stack == null)
        {
            variants = new();
            return false;
        }

        variants = Variants.FromStack(stack);

        if (inDictionary is not { Count: > 0 })
        {
            return false;
        }

        return FindByVariant(variants, inDictionary, out result);
    }

    /// <summary>
    /// Similar to FindByVariant, but returns multiple matching values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="variants"></param>
    /// <param name="inDictionary">List of keys, including keys with '::' separator used as AND operator </param>
    /// <returns>True, if value by key is found, otherwise false</returns>
    public static IEnumerable<T> FindAllByVariant<T>(this Variants variants, IDictionary<string, T> inDictionary)
    {
        Core.Api?.World?.FrameProfiler?.Enter("AttributeRenderingLibrary.FindAllByVariant");
        if (variants == null || inDictionary is not { Count: > 0 })
        {
            Core.Api?.World?.FrameProfiler?.Leave();
            yield break;
        }

        List<string> variantAsStringArray = variants.GetAsStringArray();
        foreach ((string key, T value) in inDictionary)
        {
            string[] keys = key.Contains("::") ? key.Split("::") : [key];
            if (keys.All(k => variantAsStringArray.Any(v => WildcardUtil.Match(k, v))))
            {
                yield return value;
            }
        }

        Core.Api?.World?.FrameProfiler?.Leave();
    }

    /// <summary>
    /// Same as FindByVariant, but returns a value instead of bool. Returns a default value when no match is found.
    /// </summary>
    public static T? GetByVariant<T>(this ItemStack? stack, Dictionary<string, T>? inDictionary, T? defaultValue = default)
    {
        return stack?.FindByVariant(inDictionary!, out var result) == true ? result : defaultValue;
    }

    /// <summary>
    /// Same as FindByVariant, but returns a value instead of bool. Returns a default value using lazy evaluation.
    /// </summary>
    public static T GetByVariant<T>(this ItemStack? stack, Dictionary<string, T>? inDictionary, Func<T> defaultValue)
    {
        return stack?.FindByVariant(inDictionary!, out var result) == true ? result : defaultValue();
    }

    /// <summary>
    /// Same as FindByVariant, but returns a value instead of bool. Returns a default value when no match is found.
    /// </summary>
    public static IEnumerable<T>? GetByVariant<T>(this ItemStack? stack, Dictionary<string, IEnumerable<T>>? inDictionary, IEnumerable<T>? defaultValue = default)
    {
        return stack?.FindByVariant(inDictionary!, out var result) == true ? result : defaultValue;
    }

    /// <summary>
    /// Same as FindByVariant, but returns a value instead of bool. Returns a default value using lazy evaluation.
    /// </summary>
    public static IEnumerable<T> GetByVariant<T>(this ItemStack? stack, Dictionary<string, IEnumerable<T>>? inDictionary, Func<IEnumerable<T>> defaultValue)
    {
        return stack?.FindByVariant(inDictionary!, out var result) == true ? result : defaultValue();
    }

    public static bool IsTrue(this Variants variants, Dictionary<string, bool> inDictionary)
    {
        return variants.FindByVariant(inDictionary, out bool result) && result;
    }

    /// <summary>
    /// Overwrites the variants of the input <see cref="ItemStack"/> based on the specified parameters.
    /// If the <paramref name="variants"/> argument is null, the variants from the <paramref name="oldStack"/> are cloned and used.
    /// </summary>
    /// <param name="oldStack">
    /// The original <see cref="ItemStack"/> whose variants are used as the base if <paramref name="variants"/> is null.
    /// </param>
    /// <param name="newStack">
    /// An output parameter that returns the modified <see cref="ItemStack"/> with updated variants.
    /// </param>
    /// <param name="setVariants">
    /// A dictionary of attribute key-value pairs to add or update in the variants.
    /// If null, no attributes are added.
    /// </param>
    /// <param name="removeVariants">
    /// A list of attribute keys to remove from the variants.
    /// If null, no attributes are removed.
    /// </param>
    /// <param name="variants">
    /// (Optional) A <see cref="Variants"/> object to use for the new stack. 
    /// If null, the variants from <paramref name="oldStack"/> are cloned and used.
    /// </param>
    /// <remarks>
    /// The method ensures that the original variants and stack remain unmodified by cloning them before applying changes.
    /// </remarks>
    public static void OverwriteVariants(this ItemStack oldStack, out ItemStack newStack, Dictionary<string, string> setVariants = null!, List<string> removeVariants = null!, Variants variants = null!)
    {
        newStack = oldStack.Clone();
        Variants newVariants = variants?.Clone() ?? Variants.FromStack(newStack);

        newVariants.Set(setVariants);
        newVariants.RemoveKeys(removeVariants?.ToArray()!);
        newVariants.ToStack(newStack);
    }

    public static void AppendTranslatedText(this Variants variants, StringBuilder sb, List<object> entries)
    {
        foreach (object entry in entries)
        {
            switch (entry)
            {
                case string str:
                    {
                        sb.Append(Lang.GetMatching(key: variants.ReplacePlaceholders(str)));
                    }
                    break;
                case JArray array:
                    {
                        string result = variants.TranslateJArray(array);
                        if (!string.IsNullOrEmpty(result))
                        {
                            sb.Append(result);
                        }
                    }
                    break;
            }
        }
    }

    private static string TranslateJArray(this Variants variants, JArray array)
    {
        if (!array.Any()) return string.Empty;

        object[] args = [.. array.Skip(1).Select(arg =>
        {
            return arg.Type switch
            {
                JTokenType.String => Lang.GetMatching(key: variants.ReplacePlaceholders(arg.ToString())),
                JTokenType.Array  => variants.TranslateJArray(arg.ToObject<JArray>()!),
                _                 => (object)arg
            };
        })];

        string resultKey = variants.ReplacePlaceholders(array[0].ToString());

        if (Lang.HasTranslation(resultKey))
        {
            return Lang.GetMatching(resultKey, args);
        }
        else
        {
            return string.Format(resultKey, args);
        }
    }

    public static string GetName(this Variants variants, List<object> entries)
    {
        if (!variants.Any || entries is not { Count: > 0 })
        {
            return "";
        }

        StringBuilder sb = new();
        variants.AppendTranslatedText(sb, entries);
        return sb.ToString();
    }

    public static void GetDescription(this Variants variants, StringBuilder sb, List<object> entries)
    {
        if (!variants.Any || entries is not { Count: > 0 })
        {
            return;
        }

        variants.AppendTranslatedText(sb, entries);
    }

    public static void GetDebugDescription(this Variants variants, StringBuilder sb, bool withDebugInfo = false)
    {
        if (!ClientSettings.ExtendedDebugInfo) return;

        if (!variants.Any)
        {
            sb.AppendLine("<font color=\"#bbbbbb\">ARL Variants: None</font>");
            return;
        }

        sb.AppendLine("<font color=\"#bbbbbb\">ARL Variants:");
        foreach (string keyVal in variants.GetAsStringArray())
        {
            sb.AppendLine($"- {keyVal}");
        }
        sb.AppendLine("</font>");
    }
}