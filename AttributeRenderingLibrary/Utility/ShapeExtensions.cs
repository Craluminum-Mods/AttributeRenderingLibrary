using System.Collections.Generic;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

public static class ShapeExtensions
{
    public static CompositeShape? RemoveNonExistingOverlays(this CompositeShape? shape, ICoreAPI api)
    {
        if (shape?.Overlays is { Length: > 0 })
        {
            List<CompositeShape> _overlays = [];
            foreach (CompositeShape overlay in shape.Overlays)
            {
                if (api.Assets.Exists(overlay.Base.Clone().CopyWithPathPrefixAndAppendixOnce("shapes/", ".json")))
                {
                    _overlays.Add(overlay);
                }
            }
            shape.Overlays = [.. _overlays];
        }

        return shape;
    }
}