using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AttributeRenderingLibrary;

public class Transforms
{
    public VariantSelectionList<ModelTransform> GuiTransform { get; set; }
    public VariantSelectionList<ModelTransform> TpHandTransform { get; set; }
    public VariantSelectionList<ModelTransform> TpOffHandTransform { get; set; }
    public VariantSelectionList<ModelTransform> GroundTransform { get; set; }

    public static Transforms LoadFrom(JsonObject json) => new()
    {
        GuiTransform = VariantSelectionList<ModelTransform>.LoadFrom(json[nameof(GuiTransform)]),
        TpHandTransform = VariantSelectionList<ModelTransform>.LoadFrom(json[nameof(TpHandTransform)]),
        TpOffHandTransform = VariantSelectionList<ModelTransform>.LoadFrom(json[nameof(TpOffHandTransform)]),
        GroundTransform = VariantSelectionList<ModelTransform>.LoadFrom(json[nameof(GroundTransform)]),
    };
}
