
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace AttributeRenderingLibrary.Utility.HarmonyTools;

public static class DynamicTargetTools
{
    public static IEnumerable<(Assembly assembly, AssemblyDefinition monoAssembly)> GetAssembliesToScan(AssemblyName[]? referencing = null, Assembly[]? exclude = null)
    {
        var assemblies = AccessTools.AllAssemblies();

        if(exclude is not null && exclude.Length > 0) assemblies = assemblies.Except(exclude);
        if(referencing is not null && referencing.Length > 0) assemblies = assemblies.Where(assembly =>  assembly.GetReferencedAssemblies().Any(a1 => referencing.Any(a2 => a1.FullName == a2.FullName)));

        foreach(var assembly in assemblies)
        {
            AssemblyDefinition? assemblyDefinition = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(assembly.Location))
                {
                    assemblyDefinition = AssemblyDefinition.ReadAssembly(assembly.Location);
                }
            }
            catch
            {
                //Ignore assemblies that cannot be read
            }

            if(assemblyDefinition is not null)
            {
                yield return (assembly, assemblyDefinition);
            }
        }
    }

    public static IEnumerable<MethodBase> GetMethodsUsing(MethodBase[]? methods = null, FieldInfo[]? fields = null, Assembly[]? exclude = null, ILogger? logger = null)
    {
        var referencing = new HashSet<Assembly>();
        if(methods is not null) referencing.AddRange(methods.Select(static m => m.DeclaringType?.Assembly).NotNull());
        if(fields is not null) referencing.AddRange(fields.Select(static f => f.DeclaringType?.Assembly).NotNull());

        var scanTargets = GetAssembliesToScan(
            [.. referencing.Select(a => a.GetName())],
            exclude
        );

        foreach((var assembly, var monoAssembly) in scanTargets)
        {
            foreach (var type in monoAssembly.Modules.SelectMany(m => m.Types))
            {
                
                foreach (var method in type.Methods)
                {
                    //Skip unpatchable methods (like abstract/interface methods) and generic methods (these are not supported by harmony)
                    if (!method.HasBody || method.HasGenericParameters || !method.Uses(methods, fields)) continue;
                    
                    //TODO maybe improve matching for overloads
                    var candidates = AccessTools.GetTypesFromAssembly(assembly)
                        .Where(realType => realType.Name == type.Name)
                        .SelectMany(realType =>
                            realType.GetMethods(AccessTools.allDeclared)
                                .Cast<MethodBase>()
                                .Concat(realType.GetConstructors(AccessTools.allDeclared).Cast<MethodBase>())
                        )
                        .Where(realMethod => realMethod.Name == method.Name)
                        .ToList();

                    if (candidates.Count == 0)
                    {
                        logger?.VerboseDebug("failed to find real method of {0}, {1}", assembly.FullName, method.FullName);
                        break;
                    }

                    // Prefer an exact signature match; fall back to the only candidate if signature info is unavailable
                    MethodBase? realMethod = candidates.FirstOrDefault(candidate => ParametersMatch(method, candidate))
                        ?? (candidates.Count == 1 ? candidates[0] : null);

                    if (realMethod is null)
                    {
                        logger?.VerboseDebug("failed to find real method of {0}, {1}", assembly.FullName, method.FullName);
                        break;
                    }

                    yield return realMethod;
                }
            }
        }
    }

    private static IEnumerable<T> NotNull<T>(this IEnumerable<T?> maybeNull) => maybeNull.Where(static obj => obj is not null)!;

    public static bool Uses(this MethodDefinition method, MethodBase[]? methods, FieldInfo[]? fields)
    {
        foreach(var instruction in method.Body.Instructions)
        {
            if(fields is not null && instruction.Operand is FieldReference fieldRef && fields.Any(targetField => FieldMatches(fieldRef, targetField))) return true;
            if(methods is not null && instruction.Operand is MethodReference methodRef && methods.Any(targetField => MethodMatches(methodRef, targetField))) return true;
        }

        return false;
    }

    private static bool ParametersMatch(MethodReference cref, MethodBase rmethod)
    {
        var rparams = rmethod.GetParameters();
        if (cref.Parameters.Count != rparams.Length)
            return false;
    
        for (int i = 0; i < rparams.Length; i++)
        {
            var cp = cref.Parameters[i].ParameterType;
            var rp = rparams[i].ParameterType;
    
            if (!TypeMatches(cp, rp))
                return false;
        }
        return true;
    }

    private static bool TypesMatch(TypeReference cref, Type? rtype)
    {
        if (cref.Name != rtype?.Name || cref.Namespace != rtype.Namespace) return false;
    
        if (cref is GenericInstanceType git) return git.GenericArguments.Count == rtype.GenericTypeArguments.Length;
    
        return true;
    }
    
    private static bool TypeMatches(TypeReference cref, Type? rtype)
    {
        if (cref.Name != rtype?.Name || cref.Namespace != rtype.Namespace) return false;
    
        if (cref is ArrayType ca && rtype.IsArray) return TypeMatches(ca.ElementType, rtype.GetElementType()) && ca.Rank == rtype.GetArrayRank();
    
        if (cref is GenericInstanceType git)
        {
            if (!rtype.IsGenericType) return false;
            
            var args = rtype.GetGenericArguments();
            if (args.Length != git.GenericArguments.Count) return false;
            
            for (int j = 0; j < args.Length; j++)
            {
                if (!TypeMatches(git.GenericArguments[j], args[j])) return false;
            }
        }
    
        return true;
    }

    private static bool MethodMatches(MethodReference cref, MethodBase rmethod)
    {
        if (cref.Name != rmethod.Name) return false;

        if (!TypesMatch(cref.DeclaringType, rmethod.DeclaringType)) return false;

        if (cref.HasGenericParameters && (!rmethod.IsGenericMethod || cref.GenericParameters.Count != rmethod.GetGenericArguments().Length))
        {
            return false;
        }

        return ParametersMatch(cref, rmethod);
    }

    private static bool FieldMatches(FieldReference fd, FieldInfo fi) => 
        fd.Name == fi.Name 
        && (fd.DeclaringType?.FullName) == (fi.DeclaringType?.FullName)
        && fd.FieldType.FullName == fi.FieldType.FullName;
}
