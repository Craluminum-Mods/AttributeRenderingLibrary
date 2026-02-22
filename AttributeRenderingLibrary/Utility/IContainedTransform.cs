using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public interface IContainedTransform
{
    ModelTransform? GetTransform(BlockEntityDisplay be, string attributeTransformCode, ItemStack itemStack);
}