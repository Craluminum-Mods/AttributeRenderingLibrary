using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace AttributeRenderingLibrary;

/// <summary>
/// Collection of attributes very similar to VariantGroups, that are stored in local ItemStack / BlockEntity, instead of global CollectibleObject (Block, Item)
/// </summary>
public class Variants
{
    public const string RootAttributeName = "types";

    protected Dictionary<string, string> Elements { get; set; } = new();

    public int Count => Elements.Count;
    public bool Any => Elements.Any();

    public List<string> GetAsStringArray()
    {
        return Elements.Select(x => $"{x.Key}-{x.Value}").ToList();
    }

    public string Get(string key)
    {
        return Elements.GetValueSafe(key);
    }

    public void Set(string key, string value)
    {
        if (Elements.ContainsKey(key))
        {
            Elements[key] = value;
            return;
        }
        Elements.TryAdd(key, value);
    }

    public void Set(params Variant[] newVariants)
    {
        if (newVariants == null || newVariants.Length == 0)
        {
            return;
        }

        foreach (Variant variant in newVariants)
        {
            Set(variant.Key, variant.Value);
        }
    }

    public void Set(Dictionary<string, string> newVariants)
    {
        if (newVariants == null || newVariants.Count == 0)
        {
            return;
        }

        foreach ((string key, string value) in newVariants)
        {
            Set(key, value);
        }
    }

    public void RemoveKeys(params string[] keys)
    {
        if (keys == null || keys.Length == 0)
        {
            return;
        }

        Elements.RemoveAllByKey(key => keys.Contains(key));
    }

    public static Variants FromTreeAttribute(ITreeAttribute rootTree)
    {
        Variants variants = new Variants();
        if (!rootTree.HasAttribute(RootAttributeName))
        {
            return variants;
        }

        ITreeAttribute typesTree = rootTree.GetTreeAttribute(RootAttributeName);
        foreach (string key in typesTree.Select(x => x.Key).Where(key => !variants.Elements.ContainsKey(key)))
        {
            variants.Elements.Add(key, typesTree.GetString(key));
        }
        return variants;
    }

    /// <summary> Overwrites tree </summary>
    public void ToTreeAttribute(ITreeAttribute rootTree)
    {
        rootTree.RemoveAttribute(RootAttributeName);
        ITreeAttribute typesTree = rootTree.GetOrAddTreeAttribute(RootAttributeName);
        foreach ((string key, string val) in Elements)
        {
            typesTree.SetString(key, val);
        }
    }

    public static Variants FromStack(ItemStack stack)
    {
        return FromTreeAttribute(stack.Attributes);
    }

    /// <summary> Overwrites tree </summary>
    public void ToStack(ItemStack stack)
    {
        ToTreeAttribute(stack.Attributes);
    }

    public string ReplacePlaceholders(string input)
    {
        foreach ((string key, string value) in Elements)
        {
            input = input.Replace($"{{{key}}}", value);
        }
        return input;
    }

    public AssetLocation ReplacePlaceholders(AssetLocation location)
    {
        if (location.Domain != "game")
        {
            location.Path = ReplacePlaceholders(location.Path);
            return location;
        }
        location = new AssetLocation(ReplacePlaceholders(location.Path));
        if (!location.HasDomain())
        {
            location.Domain = "game";
        }

        return location;
    }

    public CompositeShape ReplacePlaceholders(CompositeShape cshape)
    {
        cshape.Base = ReplacePlaceholders(cshape.Base);

        if (cshape.Overlays != null && cshape.Overlays.Length > 0)
        {
            for (int i = 0; i < cshape.Overlays.Length; i++)
            {
                cshape.Overlays[i].Base = ReplacePlaceholders(cshape.Overlays[i].Base);
            }
        }

        return cshape;
    }

    public CompositeTexture ReplacePlaceholders(CompositeTexture ctex)
    {
        foreach ((string key, string value) in Elements)
        {
            ctex.FillPlaceholder($"{{{key}}}", value);
        }
        return ctex;
    }

    public JsonItemStack ReplacePlaceholders(JsonItemStack jstack)
    {
        foreach ((string key, string value) in Elements)
        {
            jstack.FillPlaceHolder(key, value);
        }
        return jstack;
    }

    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        if (Elements.Any())
        {
            result.Append(string.Join('-', Elements.Select(x => $"{x.Key}-{x.Value}")));
        }
        return result.ToString();
    }

    public Variants Clone()
    {
        return new Variants()
        {
            Elements = Elements
        };
    }
}