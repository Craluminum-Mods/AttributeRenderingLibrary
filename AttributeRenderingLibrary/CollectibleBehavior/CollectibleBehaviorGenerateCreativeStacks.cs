using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorGenerateCreativeStacks : CollectibleBehavior
{
    public RegistryObjectVariantGroup[] AttributeVariantGroups { get; protected set; }
    public List<string> AllowedCombinations { get; protected set; } = null;
    public List<string> SkipCombinations { get; protected set; } = null;
    public Dictionary<string, string[]> CreativeInventory { get; protected set; } = null;

    public CollectibleBehaviorGenerateCreativeStacks(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties != null)
        {
            AttributeVariantGroups = properties["variantgroups"].AsObject<RegistryObjectVariantGroup[]>(defaultValue: null);
            CreativeInventory = properties["creativeinventory"].AsObject<Dictionary<string, string[]>>(defaultValue: new());
        }
    }
}
