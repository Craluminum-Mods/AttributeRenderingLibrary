using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary;

public static class CuboidExtensions
{
    public static Cuboidf[] ToCuboidf(this RotatableCube[] cubes)
    {
        Cuboidf[] newCubes = new Cuboidf[cubes.Length];
        for (int i = 0; i < cubes.Length; i++)
        {
            newCubes[i] = cubes[i].RotatedCopy();
        }
        return newCubes;
    }
}
