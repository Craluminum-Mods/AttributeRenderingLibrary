
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary.HarmonyPatches;

public static partial class AttributeRedirectionPatches
{
    private static IEnumerable<(Assembly, AssemblyDefinition)> GetTargetAssemblies()
    {
        var targetAssemblyName = typeof(ItemStack).Assembly.GetName().Name;

        return AppDomain.CurrentDomain.GetAssemblies()
        //Skip any dynamic assembly (cause I don't know how to read the assembly definition of these)
        .Where(assembly => !string.IsNullOrEmpty(assembly.Location))
        //Skip anything that does not reference vintage story
        .Where(assembly => assembly.GetName().Name == targetAssemblyName || Array.Exists(assembly.GetReferencedAssemblies(), assembly => assembly.Name == targetAssemblyName))
        .Select(assembly => (assembly, AssemblyDefinition.ReadAssembly(assembly.Location)));
    }

    private static IEnumerable<MethodBase> FindTargetMethods(Assembly assembly, AssemblyDefinition monoAssembly, ILogger logger)
    {
        foreach (var type in monoAssembly.Modules.SelectMany(m => m.Types))
        {
            if(type.Name == nameof(ItemStack)) continue;

            foreach (var method in type.Methods)
            {
                //Skip unpatchable methods (like abstract/interface methods) and generic methods (these are not supported by harmony)
                if (!method.HasBody || method.HasGenericParameters || !AccessesAttributes(method)) continue;

                var realMethods = assembly.GetTypes()
                    .First(realType => realType.Name == type.Name)
                    .GetMethods(AccessTools.allDeclared)
                    .Where(realMethod => realMethod.Name == method.Name)
                    .ToList();

                if (realMethods.Count != 1)
                {
                    logger.VerboseDebug("AttributeRedirectionPatches failed to find real method of {0}, {1}", assembly.FullName, method.FullName);
                    break;
                }

                yield return realMethods[0];
            }
        }
    }

    private static bool AccessesAttributes(MethodDefinition method)
    {
        for (int i = 1; i < method.Body.Instructions.Count; i++)
        {
            Instruction instruction = method.Body.Instructions[i];
            if (instruction.OpCode != OpCodes.Ldfld || instruction.Operand is not FieldReference { DeclaringType.Name: nameof(CollectibleObject), Name: nameof(CollectibleObject.Attributes) }) continue;

            instruction = method.Body.Instructions[i - 1];

            if(!LoadsCollectibleFromItemStack(instruction)) continue;
            //TODO maybe handle the edge case where someone loads an Collectible/Item/Block and then adds a null check on that.

            return true;
        }

        return false;
    }

    private static bool LoadsCollectibleFromItemStack(Instruction instruction) => instruction.Operand is MethodReference {
        DeclaringType.Name: nameof(ItemStack), 
        Name: "get_Collectible" or "get_Block" or "get_Item" 
    };
}
