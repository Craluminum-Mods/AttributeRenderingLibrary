using AttributeRenderingLibrary.Utility.Creativestacks;
using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace AttributeRenderingLibrary;

public class Core : ModSystem
{
    private Harmony HarmonyInstance => new Harmony(Mod.Info.ModID);

    public override void StartPre(ICoreAPI api)
    {
        HarmonyInstance.PatchAllUncategorized();
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("AttributeRenderingLibrary.ItemShapeTexturesFromAttributes", typeof(ItemShapeTexturesFromAttributes));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.ShapeTexturesFromAttributes", typeof(CollectibleBehaviorShapeTexturesFromAttributes));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.ContainedTransform", typeof(CollectibleBehaviorContainedTransform));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.AttachableToEntityTyped", typeof(CollectibleBehaviorAttachableToEntityTyped));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HeldBagTyped", typeof(CollectibleBehaviorHeldBagTyped));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.GenerateCreativeStacks", typeof(CollectibleBehaviorGenerateCreativeStacks));
        Mod.Logger.Event("started '{0}' mod", Mod.Info.Name);
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);

        if (api.Side.IsServer())
        {
            CreateCreativeStacks(api as ICoreServerAPI);
        }
    }

    private void CreateCreativeStacks(ICoreServerAPI sapi)
    {
        VariantLoader loader = new(sapi);
        loader.CollectCollectibleObjectsToGenerate();
        loader.CollectVariantsFromWorldProperties();
        loader.ComposeVariants();
    }
}
