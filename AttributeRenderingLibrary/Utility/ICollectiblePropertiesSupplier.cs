using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

/// <summary>
/// Other properties that aren't handled by <see cref="CollectibleObject"/>
/// </summary>
public interface ICollectiblePropertiesSupplier
{
    JuiceableProperties GetJuiceableProperties(ItemStack stack, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    DistillationProps GetDistillationProperties(ItemStack stack, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }
}

/// <summary>
/// <see cref="CollectibleObject"/> properties that aren't handled by <see cref="CollectibleObject.CollectibleBehaviors"/>
/// </summary>
public interface IPropertiesSupplier : ICollectiblePropertiesSupplier
{
    TagSet GetTags(ItemStack stack, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return new();
    }

    byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return [];
    }

    EnumItemDamageSource[] GetDamagedBy(ItemSlot slot, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return [];
    }

    EnumTool? GetTool(ItemSlot slot, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    int GetToolTier(ItemSlot slot, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return 0;
    }

    CombustibleProperties GetCombustibleProperties(IWorldAccessor world, ItemStack stack, BlockPos pos, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack stack, Entity forEntity, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    GrindingProperties GetGrindingProperties(IWorldAccessor world, ItemStack stack, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    CrushingProperties GetCrushingProperties(IWorldAccessor world, ItemStack stack, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack stack, Entity forEntity, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    int GetRandomColor(ICoreClientAPI capi, ItemStack stack, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return 0;
    }
}

/// <summary>
/// <see cref="Block"/> properties that aren't handled by <see cref="Block.BlockBehaviors"/>
/// </summary>
public interface IBlockPropertiesSupplier : IPropertiesSupplier
{
    int GetRequiredMiningTier(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return 0;
    }

    EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return EnumBlockMaterial.Stone;
    }

    int GetColor(ICoreClientAPI capi, BlockPos pos, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return 0;
    }
}