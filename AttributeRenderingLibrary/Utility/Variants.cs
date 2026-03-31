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

    protected SortedDictionary<string, string> Elements { get; set; } = [];

    public int Count => Elements.Count;
    public bool Any => Elements.Count != 0;

    public List<string> GetAsStringArray()
    {
        return [.. Elements.Select(x => $"{x.Key}-{x.Value}")];
    }

    public string? this[string key]
    {
        get => Elements.GetValueOrDefault(key);
        set => Elements[key] = value!;
    }

    [Obsolete("Use the indexer instead")]
    public string Get(string key) => this[key]!;

    [Obsolete("Use the indexer instead")]
    public void Set(string key, string value) => this[key] = value;

    public void Set(params Variant[] newVariants)
    {
        if (newVariants is not { Length: > 0 })
        {
            return;
        }

        foreach (Variant variant in newVariants)
        {
            this[variant.Key] = variant.Value;
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
            this[key] =  value;
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
        foreach (var (key, value) in otherVariants.Elements)
        {
            if (ignoreKeys?.Contains(key) == true) continue;

            this[key] = value;
        }
    }

    public static Variants FromTreeAttribute(ITreeAttribute rootTree)
    {
        Variants variants = new();
        var tree = rootTree.GetTreeAttribute(RootAttributeName);
        if (tree is not null)
        {
            foreach((var key, var item) in tree)
            {
                if(item is not StringAttribute strAttr) continue;
                variants.Elements.Add(key, string.Intern(strAttr.value));
            }
        }
        return variants;
    }

    /// <summary> Overwrites tree </summary>
    public void ToTreeAttribute(ITreeAttribute rootTree)
    {
        rootTree.RemoveAttribute(RootAttributeName);

        if (Elements is not { Count: > 0 }) return;

        ITreeAttribute typesTree = rootTree.GetOrAddTreeAttribute(RootAttributeName);
        foreach (var (key, val) in Elements)
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
        if (input is not { Length: > 0 }) return input!;

        foreach ((string key, string value) in Elements)
        {
            input = input.Replace($"{{{key}}}", value);
        }
        return input;
    }

    public string[] ReplacePlaceholders(string[] input)
    {
        if (input is not { Length: > 0 }) return input!;

        for (int i = 0; i < input.Length; i++)
        {
            input[i] = ReplacePlaceholders(input[i]);
        }
        return input;
    }

    /// <summary>
    /// Mutates the provided <see cref="AssetLocation"/> by replacing placeholders in its Path.
    /// <para><br/>WARNING: This modifies the original reference. If shared, it changes globally.</para>
    /// </summary>
    /// <returns>The same <see cref="AssetLocation"/> instance with a replaced path.</returns>
    public AssetLocation ReplacePlaceholders(AssetLocation location)
    {
        if (location is not { Path: not null }) return location!;

        location.Path = ReplacePlaceholders(location.Path);

        return location;
    }

    /// <summary>
    /// Mutates the provided <see cref="CompositeShape"/> by replacing placeholders in its Base, Overlays, Alternates, IgnoreElements and SelectiveElements.
    /// <para><br/>WARNING: This modifies the original reference. If the shape is shared (e.g., from a Block Type),
    /// it will be changed globally.</para>
    /// </summary>
    /// <returns>The same <see cref="CompositeShape"/> instance with replaced placeholders.</returns>
    public CompositeShape ReplacePlaceholders(CompositeShape cshape)
    {
        if (cshape is not { Base: not null }) return cshape!;

        cshape.Base = ReplacePlaceholders(cshape.Base);

        if (cshape.Overlays is { Length: > 0 })
        {
            for (int i = 0; i < cshape.Overlays.Length; i++)
            {
                cshape.Overlays[i].Base = ReplacePlaceholders(cshape.Overlays[i].Base);
            }
        }

        if (cshape.Alternates is { Length: > 0 })
        {
            for (int i = 0; i < cshape.Alternates.Length; i++)
            {
                cshape.Alternates[i].Base = ReplacePlaceholders(cshape.Alternates[i].Base);
            }
        }

        if (cshape.IgnoreElements is { Length: > 0 })
        {
            cshape.IgnoreElements = ReplacePlaceholders(cshape.IgnoreElements);
        }

        if (cshape.SelectiveElements is { Length: > 0 })
        {
            cshape.SelectiveElements = ReplacePlaceholders(cshape.SelectiveElements);
        }

        return cshape;
    }

    /// <summary>
    /// Mutates the provided <see cref="CompositeTexture"/> by replacing placeholders in Base.Path, BlendedOverlays, Alternates and Tiles.
    /// <para><br/>WARNING: This modifies the original texture definition. If this texture is part of 
    /// a shared block or item type, the change will apply globally.</para>
    /// </summary>
    /// <returns>The same <see cref="CompositeTexture"/> instance with replaced placeholders.</returns>
    public CompositeTexture ReplacePlaceholders(CompositeTexture ctex)
    {
        if (ctex is not { Base: not null }) return ctex!;

        foreach ((string key, string value) in Elements)
        {
            ctex.FillPlaceholder($"{{{key}}}", value);
        }
        return ctex;
    }

    /// <summary>
    /// Mutates the provided <see cref="JsonItemStack"/>, replacing placeholders in its Code and Attributes.
    /// <para><br/>WARNING: Modifies the original reference. This can lead to unexpected behavior if 
    /// the stack is shared across multiple instances.</para>
    /// </summary>
    /// <returns>The same <see cref="JsonItemStack"/> instance with replaced placeholders.</returns>
    public JsonItemStack ReplacePlaceholders(JsonItemStack jstack)
    {
        if (jstack is not { Code: not null }) return jstack!;

        foreach ((string key, string value) in Elements)
        {
            jstack.FillPlaceHolder(key, value);
        }
        return jstack;
    }

    /// <summary>
    /// Mutates the provided <see cref="BlockDropItemStack"/>, replacing placeholders in its Code and Attributes.
    /// <para><br/>WARNING: Modifies the original reference. This can lead to unexpected behavior if 
    /// the stack is shared across multiple instances.</para>
    /// </summary>
    /// <returns>The same <see cref="BlockDropItemStack"/> instance with replaced placeholders.</returns>
    public BlockDropItemStack ReplacePlaceholders(BlockDropItemStack bdstack)
    {
        if (bdstack is not { Code: not null }) return bdstack!;

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
        StringBuilder result = new();
        if (Elements is { Count: > 0 })
        {
            _ = result.Append(string.Join('-', GetAsStringArray()));
        }
        return result.ToString();
    }

    public Variants Clone()
    {
        return new() { Elements = Elements };
    }
}