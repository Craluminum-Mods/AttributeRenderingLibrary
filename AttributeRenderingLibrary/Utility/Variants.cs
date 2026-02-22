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

    protected SortedDictionary<string, string> Elements { get; set; } = new();

    public int Count => Elements.Count;
    public bool Any => Elements.Count != 0;

    public List<string> GetAsStringArray()
    {
        return Elements.Select(x => $"{x.Key}-{x.Value}").ToList();
    }

    public string Get(string key)
    {
        return Elements.GetValueOrDefault(key);
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
        if (newVariants is not { Length: > 0 })
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
        if (newVariants is not { Count: > 0 })
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
        if (keys is not { Length: > 0 })
        {
            return;
        }

        Elements.RemoveAllByKey(key => keys.Contains(key));
    }

    public bool ContainsKey(string key)
    {
        return Elements.ContainsKey(key);
    }

    public void MergeVariants(Variants otherVariants, params string[] ignoreKeys)
    {
        foreach ((string key, string value) in otherVariants.Elements)
        {
            if (ignoreKeys is { Length: > 0 } && ignoreKeys.Contains(key))
            {
                continue;
            }

            Set(key, value);
        }
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

    public string[] ReplacePlaceholders(string[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = ReplacePlaceholders(input[i]);
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

        if (cshape.Overlays is { Length: > 0 })
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

    public BlockDropItemStack ReplacePlaceholders(BlockDropItemStack bdstack)
    {
        bdstack.Code = ReplacePlaceholders(bdstack.Code);

        if (bdstack.Attributes != null)
        {
            foreach ((string key, string value) in Elements)
            {
                bdstack.Attributes.FillPlaceHolder(key, value);
            }
        }
        return bdstack;
    }

    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        if (Elements is { Count: > 0 })
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