using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AttributeRenderingLibrary;

public interface IHandbookARL
{
    public bool ExcludeFromHandbookSearch(ItemStack stack);
    public bool IncludeInHandbookSearch(ItemStack stack);
}

public class CollectibleBehaviorHandbook(CollectibleObject collObj) : CollectibleBehavior(collObj), IHandbookARL
{
    public Dictionary<string, bool>? ExcludeFromHandbookSearchByType { get; protected set; }
    public Dictionary<string, bool>? IncludeInHandbookSearchByType { get; protected set; }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties is not { Count: > 0 }) return;

        ExcludeFromHandbookSearchByType = properties["excludeFromHandbookSearch"].AsObject<Dictionary<string, bool>>();
        IncludeInHandbookSearchByType = properties["includeInHandbookSearch"].AsObject<Dictionary<string, bool>>();
    }

    public virtual bool ExcludeFromHandbookSearch(ItemStack stack) => stack.GetByVariant(ExcludeFromHandbookSearchByType, false);
    public virtual bool IncludeInHandbookSearch(ItemStack stack) => stack.GetByVariant(IncludeInHandbookSearchByType, true);
}
