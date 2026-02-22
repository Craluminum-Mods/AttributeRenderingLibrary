using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary.Utility.Creativestacks;

public class CollectibleAndStackGenerationBehavior
{
    public CollectibleObject CollectibleObject { get; set; }
    public CollectibleBehaviorGenerateCreativeStacks CollectibleBehavior { get; set; }
}

public struct CombineState
{
    public EnumCombination Combine { get; set; }
    public string Code { get; set; }
    public string OnVariant { get; set; }
    public string[] States { get; set; }
}

public struct CollectibleAndStacks
{
    public CollectibleObject Collectible { get; set; }
    public JsonItemStack[] Stacks { get; set; }
    public Dictionary<string, CombineState> CompleteVariantGroups { get; set; }
}
