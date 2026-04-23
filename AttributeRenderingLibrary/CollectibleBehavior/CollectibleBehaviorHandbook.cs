using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public interface IHandbookARL
{
    public bool ExcludeFromHandbookSearch(ItemStack stack);
}

public class CollectibleBehaviorHandbook(CollectibleObject collObj) : CollectibleBehavior(collObj), IHandbookARL, IHandBookPageCodeProvider, IHandbookGrouping
{
    public Dictionary<string, bool>? ExcludeFromHandbookSearchByType { get; protected set; }
    public Dictionary<string, string>? PageCodeByType { get; protected set; }
    public Dictionary<string, string>? CodeForGroupingByType { get; protected set; }
    public Dictionary<string, string>? WildcardForGroupingByType { get; protected set; }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        LoadTypes(properties);
    }

    public virtual void LoadTypes(JsonObject properties)
    {
        if (properties is not { Count: > 0 }) return;

        ExcludeFromHandbookSearchByType = properties["excludeFromHandbookSearch"].AsObject<Dictionary<string, bool>>();
        PageCodeByType = properties["pageCode"].AsObject<Dictionary<string, string>>();
        CodeForGroupingByType = properties["codeForGrouping"].AsObject<Dictionary<string, string>>();
        WildcardForGroupingByType = properties["wildcardForGrouping"].AsObject<Dictionary<string, string>>();
    }

    public virtual bool ExcludeFromHandbookSearch(ItemStack stack) => stack.GetByVariant(ExcludeFromHandbookSearchByType, false);

    public virtual string HandbookPageCodeForStack(IWorldAccessor world, ItemStack stack)
    {
        var variants = Variants.FromStack(stack);
        if (variants.FindByVariant(PageCodeByType!, out string result) && result != null)
        {
            return variants.ReplacePlaceholders(result);
        }
        return GuiHandbookItemStackPage.PageCodeForStack(stack);
    }

    public virtual AssetLocation GetCodeForHandbookGrouping(ItemStack stack)
    {
        var variants = Variants.FromStack(stack);
        if (variants.FindByVariant(CodeForGroupingByType!, out string result) && result != null)
        {
            return variants.ReplacePlaceholders(result);
        }

        Dictionary<string, string> attributes = variants.GetElements();
        if (attributes.Count == 0)
        {
            return stack.Collectible.Code;
        }

        return AssetLocation.Create(stack.Collectible.Code.Path + "-" + string.Join("-", attributes.Values), stack.Collectible.Code.Domain);
    }

    public virtual string GetWildcardForHandbookGrouping(string wildcard, ItemStack stack)
    {
        var variants = Variants.FromStack(stack);
        if (variants.FindByVariant(WildcardForGroupingByType!, out string result) && result != null)
        {
            return variants.ReplacePlaceholders(result);
        }

        return variants.ReplacePlaceholders(wildcard);
    }
}