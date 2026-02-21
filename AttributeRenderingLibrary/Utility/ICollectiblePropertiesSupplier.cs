using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary;

/// <summary>
/// Other properties that aren't handled by <see cref="CollectibleObject"/>
/// </summary>
public interface ICollectiblePropertiesSupplier
{

}

/// <summary>
/// <see cref="CollectibleObject"/> properties that aren't handled by <see cref="CollectibleObject.CollectibleBehaviors"/>
/// </summary>
public interface IPropertiesSupplier : ICollectiblePropertiesSupplier
{
    public TagSet GetTags(ItemStack stack, ref EnumHandling handling);

    public EnumItemDamageSource[] GetDamagedBy(ItemSlot slot, ref EnumHandling handling);

    public EnumTool? GetTool(ItemSlot slot, ref EnumHandling handling);

    public int GetToolTier(ItemSlot slot, ref EnumHandling handling);

    public CombustibleProperties GetCombustibleProperties(IWorldAccessor world, ItemStack stack, BlockPos pos, ref EnumHandling handling);

    public FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack stack, Entity forEntity, ref EnumHandling handling);

    public GrindingProperties GetGrindingProperties(IWorldAccessor world, ItemStack stack, ref EnumHandling handling);

    public CrushingProperties GetCrushingProperties(IWorldAccessor world, ItemStack stack, ref EnumHandling handling);

    public TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack stack, Entity forEntity, ref EnumHandling handling);
}

/// <summary>
/// <see cref="Block"/> properties that aren't handled by <see cref="Block.BlockBehaviors"/>
/// </summary>
public interface IBlockPropertiesSupplier : IPropertiesSupplier
{
    public int GetRequiredMiningTier(IWorldAccessor world, BlockPos pos, ref EnumHandling handling);
}