using Vintagestory.API.Common;
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

}

/// <summary>
/// <see cref="Block"/> properties that aren't handled by <see cref="Block.BlockBehaviors"/>
/// </summary>
public interface IBlockPropertiesSupplier : IPropertiesSupplier
{
    public int GetRequiredMiningTier(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return 0;
    }
}