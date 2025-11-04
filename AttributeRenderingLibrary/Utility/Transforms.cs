using System.Collections.Generic;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

public class Transforms
{
    public Dictionary<string, ModelTransform> GuiTransform { get; set; }
    public Dictionary<string, ModelTransform> TpHandTransform { get; set; }
    public Dictionary<string, ModelTransform> TpOffHandTransform { get; set; }
    public Dictionary<string, ModelTransform> GroundTransform { get; set; }
}
