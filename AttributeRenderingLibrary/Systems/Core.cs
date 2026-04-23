using AttributeRenderingLibrary.HarmonyPatches;
using AttributeRenderingLibrary.Utility.Creativestacks;
using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class Core : ModSystem
{
    private Harmony HarmonyInstance => new(Mod.Info.ModID);

    public static ICoreAPI Api;

    public override void StartPre(ICoreAPI api)
    {
        Api = api;

        if (!Harmony.HasAnyPatches(HarmonyInstance.Id))
        {
            HarmonyInstance.PatchAllUncategorized();
            AttributeRedirectionPatch.ScanAndApply(HarmonyInstance, Mod.Logger);
        }

        if (api.Side.IsServer())
        {
            HarmonyInstance.PatchCategory("Server");
        }

        if (api.Side.IsClient())
        {
            HarmonyInstance.PatchCategory("Client");
        }
    }

    public override void Start(ICoreAPI api)
    {
        Mod.Logger.Event("started '{0}' mod", Mod.Info.Name);
        RegisterItems(api);
        RegisterCollectibleBehaviors(api);
        RegisterBlockBehaviors(api);
        RegisterBlockEntityBehaviors(api);
    }

    public void RegisterItems(ICoreAPI api)
    {
        api.RegisterItemClass("AttributeRenderingLibrary.ItemShapeTexturesFromAttributes", typeof(ItemShapeTexturesFromAttributes));
    }

    public void RegisterCollectibleBehaviors(ICoreAPI api)
    {
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.ShapeTexturesFromAttributes", typeof(CollectibleBehaviorShapeTexturesFromAttributes));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.WearableAttachment", typeof(AttributeRenderingLibrary.CollectibleBehaviorWearableAttachment));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.Wearable", typeof(AttributeRenderingLibrary.CollectibleBehaviorWearable));

        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.GenerateCreativeStacks", typeof(CollectibleBehaviorGenerateCreativeStacks));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.DisplayableProps", typeof(AttributeRenderingLibrary.CollectibleBehaviorDisplayableProps));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.FoodTags", typeof(AttributeRenderingLibrary.CollectibleBehaviorFoodTags));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HealingItem", typeof(AttributeRenderingLibrary.CollectibleBehaviorHealingItem));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.GroundStoredProcessable", typeof(AttributeRenderingLibrary.CollectibleBehaviorGroundStoredProcessable));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.AnvilWorkable", typeof(AttributeRenderingLibrary.CollectibleBehaviorAnvilWorkable));

        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.ContainedTransform", typeof(CollectibleBehaviorCustomTransform));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.CustomTransform", typeof(CollectibleBehaviorCustomTransform));

        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HeldBagTyped", typeof(AttributeRenderingLibrary.CollectibleBehaviorHeldBag));
        api.RegisterCollectibleBehaviorClass("AttributeRenderingLibrary.HeldBag", typeof(AttributeRenderingLibrary.CollectibleBehaviorHeldBag));
    }

    public void RegisterBlockBehaviors(ICoreAPI api)
    {
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.BlockShapeTexturesFromAttributes", typeof(BlockBehaviorShapeTexturesFromAttributes));

        // backward compatibility
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.HorizontalOrientable", typeof(Vintagestory.GameContent.BlockBehaviorHorizontalOrientable));
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.HorizontalAttachable", typeof(Vintagestory.GameContent.BlockBehaviorHorizontalAttachable));
        api.RegisterBlockBehaviorClass("AttributeRenderingLibrary.NWOrientable", typeof(Vintagestory.GameContent.BlockBehaviorNWOrientable));
    }

    public void RegisterBlockEntityBehaviors(ICoreAPI api)
    {
        api.RegisterBlockEntityBehaviorClass("AttributeRenderingLibrary.ShapeTexturesFromAttributes", typeof(BlockEntityBehaviorShapeTexturesFromAttributes));
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
        ARL_ObjectCacheUtil.ARL_ObjectCache.Clear();
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        if (api is ICoreServerAPI sapi)
        {
            CreateCreativeStacks(sapi);
        }

        foreach (Block block in api.World.Blocks)
        {
            if (block == null || block.Code == null) continue;

            AddMissingBlockEntityStuff(block);
            TryOrderBlockBehaviors(block);
        }
    }

    private void CreateCreativeStacks(ICoreServerAPI sapi)
    {
        VariantLoader loader = new(sapi);
        loader.CollectCollectibleObjectsToGenerate();
        loader.CollectVariantsFromWorldProperties();
        loader.ComposeVariants();
    }

    private void AddMissingBlockEntityStuff(Block block)
    {
        if (block.HasBehavior<BlockBehaviorShapeTexturesFromAttributes>())
        {
            block.EntityClass ??= "Generic";
        }
    }

    private void TryOrderBlockBehaviors(Block block)
    {
        if (block.HasBehavior<BlockBehaviorShapeTexturesFromAttributes>() && block.HasBehavior<BlockBehaviorHorizontalAttachable>())
        {
            int mainCIndex = block.CollectibleBehaviors.IndexOf(b => b is BlockBehaviorShapeTexturesFromAttributes);
            int otherCIndex = block.CollectibleBehaviors.IndexOf(b => b is BlockBehaviorHorizontalAttachable);
            if (mainCIndex >= otherCIndex)
            {
                Swap(block.CollectibleBehaviors, mainCIndex, otherCIndex);
            }

            int mainBIndex = block.BlockBehaviors.IndexOf(b => b is BlockBehaviorShapeTexturesFromAttributes);
            int otherBIndex = block.BlockBehaviors.IndexOf(b => b is BlockBehaviorHorizontalAttachable);
            if (mainBIndex > otherBIndex)
            {
                Swap(block.CollectibleBehaviors, mainBIndex, otherBIndex);
            }
        }

        if (block.HasBehavior<BlockBehaviorShapeTexturesFromAttributes>() && block.HasBehavior<BlockBehaviorNWOrientable>())
        {
            int mainCIndex = block.CollectibleBehaviors.IndexOf(b => b is BlockBehaviorShapeTexturesFromAttributes);
            int otherCIndex = block.CollectibleBehaviors.IndexOf(b => b is BlockBehaviorNWOrientable);
            if (mainCIndex > otherCIndex)
            {
                Swap(block.CollectibleBehaviors, mainCIndex, otherCIndex);
            }

            int mainBIndex = block.BlockBehaviors.IndexOf(b => b is BlockBehaviorShapeTexturesFromAttributes);
            int otherBIndex = block.BlockBehaviors.IndexOf(b => b is BlockBehaviorNWOrientable);
            if (mainBIndex > otherBIndex)
            {
                Swap(block.CollectibleBehaviors, mainBIndex, otherBIndex);
            }
        }

        if (block.HasBehavior<BlockBehaviorShapeTexturesFromAttributes>() && block.HasBehavior<BlockBehaviorHorizontalOrientable>())
        {
            int mainCIndex = block.CollectibleBehaviors.IndexOf(b => b is BlockBehaviorShapeTexturesFromAttributes);
            int otherCIndex = block.CollectibleBehaviors.IndexOf(b => b is BlockBehaviorHorizontalOrientable);
            if (mainCIndex > otherCIndex)
            {
                Swap(block.CollectibleBehaviors, mainCIndex, otherCIndex);
            }

            int mainBIndex = block.BlockBehaviors.IndexOf(b => b is BlockBehaviorShapeTexturesFromAttributes);
            int otherBIndex = block.BlockBehaviors.IndexOf(b => b is BlockBehaviorHorizontalOrientable);
            if (mainBIndex > otherBIndex)
            {
                Swap(block.CollectibleBehaviors, mainBIndex, otherBIndex);
            }
        }
    }

    public static void Swap<T>(IList<T> list, int indexA, int indexB)
    {
        T tmp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = tmp;
    }
}
