using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace AttributeRenderingLibrary;

public static class VariantExtensions
{
    public static bool FindByVariant<T>(this Variants variants, VariantSelectionList<T> inList, out T result)
    {
        Core.Api?.World.FrameProfiler.Enter("attributerenderinglibrary.findbyvariant");
        
        result = default;

        if (variants is null || inList is not { Count: > 0 })
        {
            Core.Api?.World.FrameProfiler.Leave();
            return false;
        }

        if (inList.FindFirstByVariant(variants, out result))
        {
            Core.Api?.World.FrameProfiler.Leave();
            return true;
        }

        Core.Api?.World.FrameProfiler.Leave();
        return false;
    }
    
    public static IEnumerable<T> FindAllByVariant<T>(this Variants variants, VariantSelectionList<T> inList)
    {
        Core.Api?.World.FrameProfiler.Enter("attributerenderinglibrary.findallbyvariant");
        if (variants is null || inList is not { Count: > 0 })
        {
            Core.Api?.World.FrameProfiler.Leave();
            yield break;
        }

        foreach (var entry in inList.FindAllByVariant(variants))
        {
            yield return entry;
        }

        Core.Api?.World.FrameProfiler.Leave();
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
    public static void OverwriteVariants(this ItemStack oldStack, out ItemStack newStack, Dictionary<string, string> setVariants = null, List<string> removeVariants = null, Variants variants = null)
    {
        newStack = oldStack.Clone();
        Variants newVariants = variants?.Clone() ?? Variants.FromStack(newStack);

        newVariants.Set(setVariants);
        newVariants.RemoveKeys(removeVariants?.ToArray());
        newVariants.ToStack(newStack);
    }

    public static void AppendTranslatedText(this Variants variants, StringBuilder sb, List<object> entries)
    {
        foreach (var entry in entries)
        {
            if (entry is string)
            {
                sb.Append(Lang.GetMatching(variants.ReplacePlaceholders(entry.ToString())));
            }
            else if (entry is JArray array && array.Any())
            {
                object[] args = array.Skip(1).Select(arg =>
                {
                    if (arg.Type == JTokenType.String)
                    {
                        return (object)variants.ReplacePlaceholders(arg.ToString());
                    }
                    return (object)arg;
                }).ToArray();

                string key = variants.ReplacePlaceholders(array[0].ToString());
                sb.Append(Lang.GetMatching(key, args).ToArray());
            }
        }
    }

    public static string GetName(this Variants variants, List<object> entries)
    {
        if (!variants.Any || entries == null || !entries.Any())
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        variants.AppendTranslatedText(sb, entries);
        return sb.ToString();
    }

    public static void GetDescription(this Variants variants, StringBuilder sb, List<object> entries)
    {
        if (!variants.Any || entries == null || !entries.Any())
        {
            return;
        }

        variants.AppendTranslatedText(sb, entries);
    }

    public static void GetDebugDescription(this Variants variants, StringBuilder sb, bool withDebugInfo = false)
    {
        if (!variants.Any)
        {
            return;
        }
        if (withDebugInfo)
        {
            sb.AppendLine();
            foreach (string keyVal in variants.GetAsStringArray())
            {
                sb.AppendLine($"DEBUG::{keyVal}");
            }
        }
    }
}