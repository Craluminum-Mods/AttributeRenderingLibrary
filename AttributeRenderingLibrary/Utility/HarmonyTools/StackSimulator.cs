using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace AttributeRenderingLibrary.Utility.HarmonyTools;

delegate bool TraceRequirement(StackSimulator simulator, StackEntry entry);
delegate bool TraceAdvancementRequirement(StackSimulator simulator, StackEntry entry, Trace trace);

internal class StackEntry
{
    public required CodeInstruction Instruction { get; init; }
    
    public required Type Type { get; init; }
}

internal class AlternativePath
{
    public required Stack<StackEntry> Stack { get; init; }
    public required int Pos { get; init; }
}

internal class Trace
{
    public required StackEntry Origin { get; init;}

    public HashSet<CodeInstruction> Usage = [];

    public List<StackEntry> Path { get; } = [];

    public CodeInstruction? LastFoundLoadInstruction { get; set; }

    public CodeInstruction? FindOrCreateLoadInstruction(CodeMatcher matcher)
    {
        if(LastFoundLoadInstruction is not null) return LastFoundLoadInstruction;
        
        var alreadyStored = GetAlreadyStored(Origin.Instruction);
        if(alreadyStored is not null)
        {
            LastFoundLoadInstruction = alreadyStored;
            return LastFoundLoadInstruction;
        }

        matcher.Start().MatchStartForward(new CodeMatch(instruction => instruction == Origin.Instruction));
    
        matcher.Advance(1);
        alreadyStored = GetAlreadyStored(matcher.Instruction);
        if(alreadyStored is not null)
        {
            LastFoundLoadInstruction = alreadyStored;
            return LastFoundLoadInstruction;
        }

        matcher.DeclareLocal(Origin.Type, out var localBuilder);
        matcher.Insert(
            CodeInstruction.StoreLocal(localBuilder.LocalIndex),
            CodeInstruction.LoadLocal(localBuilder.LocalIndex)
        );

        LastFoundLoadInstruction = CodeInstruction.LoadLocal(localBuilder.LocalIndex);
        return LastFoundLoadInstruction;
    }

    private static CodeInstruction? GetAlreadyStored(CodeInstruction instruction)
    {
        if(instruction.IsLdarg() || instruction.IsLdarga() || instruction.IsStarg())
        {
            return CodeInstruction.LoadArgument(instruction.ArgumentIndex());
        }

        if(instruction.IsLdloc() || instruction.IsStloc())
        {
            return CodeInstruction.LoadLocal(instruction.LocalIndex());
        }

        return null;
    }
}

internal class StackSimulator(CodeMatcher matcher, MethodBase originalMethod, TraceRequirement shouldTrace, TraceAdvancementRequirement shouldTraceAdvance)
{
    internal static readonly HashSet<OpCode> MathCodes = [
        OpCodes.Add, OpCodes.Add_Ovf, OpCodes.Add_Ovf_Un,
        OpCodes.Sub, OpCodes.Sub_Ovf, OpCodes.Sub_Ovf_Un,
        OpCodes.Mul, OpCodes.Mul_Ovf, OpCodes.Mul_Ovf_Un,
        OpCodes.Div, OpCodes.Div_Un,
        OpCodes.Rem, OpCodes.Rem_Un,
        OpCodes.Cgt, OpCodes.Cgt_Un,
        OpCodes.Clt, OpCodes.Clt_Un,
        OpCodes.And, OpCodes.Or, OpCodes.Xor, OpCodes.Shl, OpCodes.Shr, OpCodes.Shr_Un,
        OpCodes.Ceq
    ];

    internal static readonly HashSet<OpCode> UnconditionalJumpCodes = [
        OpCodes.Br, OpCodes.Br_S
    ];

    internal static readonly HashSet<OpCode> TrueFalseBranchCodes = [
        OpCodes.Brfalse, OpCodes.Brfalse_S,
        OpCodes.Brtrue, OpCodes.Brtrue_S,
    ];

    internal static readonly HashSet<OpCode> ComparitiveBranchCodes = [
        OpCodes.Beq, OpCodes.Beq_S,
        OpCodes.Bne_Un, OpCodes.Bne_Un_S,
        
        OpCodes.Bgt, OpCodes.Bgt_S,
        OpCodes.Bgt_Un, OpCodes.Bgt_Un_S,
        
        OpCodes.Blt, OpCodes.Blt_S,
        OpCodes.Blt_Un, OpCodes.Blt_Un_S,
        
        OpCodes.Ble, OpCodes.Ble_S, 
        OpCodes.Ble_Un, OpCodes.Ble_Un_S,

        OpCodes.Bge, OpCodes.Bge_S,
        OpCodes.Bge_Un, OpCodes.Bge_Un_S
    ];

    internal static readonly HashSet<OpCode> StelemCodes = [
        OpCodes.Stelem, 
        OpCodes.Stelem_I, OpCodes.Stelem_I1, OpCodes.Stelem_I2, OpCodes.Stelem_I4, OpCodes.Stelem_I8,
        OpCodes.Stelem_R4, OpCodes.Stelem_R8,
        OpCodes.Stelem_Ref
    ];

    internal static readonly HashSet<OpCode> StindCodes = [
        OpCodes.Stind_I, OpCodes.Stind_I1, OpCodes.Stind_I2, OpCodes.Stind_I4, OpCodes.Stind_I8,
        OpCodes.Stind_R4, OpCodes.Stind_R8,
        OpCodes.Stind_Ref
    ];

    internal static readonly HashSet<OpCode> LdelemCodes = [
        OpCodes.Ldelem, OpCodes.Ldelema,
        OpCodes.Ldelem_I, OpCodes.Ldelem_I1, OpCodes.Ldelem_I2, OpCodes.Ldelem_I4, OpCodes.Ldelem_I8,
        OpCodes.Ldelem_R4, OpCodes.Ldelem_R8,
        OpCodes.Ldelem_Ref,
        OpCodes.Ldelem_U1, OpCodes.Ldelem_U2, OpCodes.Ldelem_U4
    ];

    internal static readonly HashSet<OpCode> LdindCodes = [
        OpCodes.Ldind_I, OpCodes.Ldind_I2, OpCodes.Ldind_I4, OpCodes.Ldind_I8,
        OpCodes.Ldind_R4, OpCodes.Ldind_R8,
        OpCodes.Ldind_U1, OpCodes.Ldind_U2, OpCodes.Ldind_U4,
        OpCodes.Ldind_Ref
    ];

    internal static readonly HashSet<OpCode> DoNothingCodes = [
        OpCodes.Nop,
        OpCodes.Ckfinite,
        OpCodes.Box,
        OpCodes.Unbox,
        OpCodes.Unbox_Any,
        OpCodes.Rethrow,
        OpCodes.Constrained, //TODO could possibly improve StackEntry type
        OpCodes.Ret,
        OpCodes.Neg
    ];

    internal static readonly HashSet<OpCode> ConvOpcodes = [
        OpCodes.Conv_I, OpCodes.Conv_I1, OpCodes.Conv_I2, OpCodes.Conv_I4, OpCodes.Conv_I8,
        OpCodes.Conv_U, OpCodes.Conv_U1, OpCodes.Conv_U2, OpCodes.Conv_U4, OpCodes.Conv_U8,
        OpCodes.Conv_R4, OpCodes.Conv_R8,
        OpCodes.Conv_R_Un
    ];

    public readonly Type[] parameters = GetParameters(originalMethod);

    internal readonly LocalVariableInfo[] locals = originalMethod.GetMethodBody()?.LocalVariables.ToArray() ?? [];

    private static Type[] GetParameters(MethodBase originalMethod)
    {
        var parameterArgs = originalMethod.GetParameters().Select(param => param.ParameterType);
        return originalMethod.IsStatic ? [..parameterArgs] : [originalMethod.DeclaringType!, ..parameterArgs];
    }

    private readonly Stack<AlternativePath> AlternativePaths = [];

    private readonly HashSet<int> CheckedPathPos = [];

    private Stack<StackEntry> Stack = [];

    public readonly Dictionary<StackEntry, Trace> Traces = [];

    public CodeMatcher Matcher { get; } = matcher;
    public MethodBase OriginalMethod { get; } = originalMethod;

    public void Simulate()
    {
        Matcher.Start();

        do
        {
            UpdateStack();

            if(TrueFalseBranchCodes.Contains(Matcher.Opcode) || ComparitiveBranchCodes.Contains(Matcher.Opcode))
            {
                var newPathPos = Matcher.Pos + 1;
                if (CheckedPathPos.Add(newPathPos))
                {
                    AlternativePaths.Push(new AlternativePath
                    {
                        Stack = new Stack<StackEntry>(Stack.Reverse()),
                        Pos = newPathPos
                    });
                }
                
                var targetLabel = (Label)Matcher.Operand;
                Matcher.MatchStartForward(
                    new CodeMatch(instruction => instruction.labels.Contains(targetLabel))
                );
                continue;
            }

            else if(Matcher.Opcode == OpCodes.Switch)
            {
                var options = (Label[])Matcher.Operand;
                var currentPos = Matcher.Pos;
                for(var i = 0; i < options.Length; i++)
                {
                    Matcher.Start().Advance(currentPos);

                    var otherLabel = options[i];
                    Matcher.MatchStartForward(
                        new CodeMatch(instruction => instruction.labels.Contains(otherLabel))
                    );

                    if (CheckedPathPos.Add(Matcher.Pos))
                    {
                        AlternativePaths.Push(new AlternativePath
                        {
                            Stack = new Stack<StackEntry>(Stack.Reverse()),
                            Pos = Matcher.Pos
                        });
                    }
                }

                Matcher.Start().Advance(currentPos);
            }

            else if(UnconditionalJumpCodes.Contains(Matcher.Opcode))
            {
                var targetLabel = (Label)Matcher.Operand;
                Matcher.MatchStartForward(
                    new CodeMatch(instruction => instruction.labels.Contains(targetLabel))
                );
                continue;
            }

            Matcher.Advance(1);
        }
        while ((Matcher.IsValid && Matcher.Opcode != OpCodes.Ret) || MoveToNextPath());
    }

    private bool MoveToNextPath()
    {
        if(AlternativePaths.TryPop(out var path)) 
        {
            Stack = path.Stack;
            Matcher.Start().Advance(path.Pos);
            return true;
        }

        return false;
    }

    private void UpdateStack()
    {
        CodeInstruction instruction = Matcher.Instruction;

        if(instruction.blocks is { Count: > 0 })
        {
            foreach(var block in instruction.blocks)
            {
                if (block is ExceptionBlock { blockType: ExceptionBlockType.BeginCatchBlock or ExceptionBlockType.BeginExceptFilterBlock } exceptionBlock)
                {
                    Push(exceptionBlock.catchType, instruction);
                }
            }
        }

        if (instruction.opcode == OpCodes.Dup)
        {
            Stack.Push(Stack.Peek());
            return;
        }

        else if(instruction.opcode == OpCodes.Pop)
        {
            Pop();
            return;
        }

        else if(instruction.opcode == OpCodes.Ldnull)
        {
            Push(typeof(object), instruction);
            return;
        }

        else if(instruction.opcode == OpCodes.Ldtoken)
        {
            Push(
                instruction.operand switch
                {
                    MethodBase => typeof(RuntimeMethodHandle),
                    FieldInfo => typeof(RuntimeFieldHandle),
                    Type => typeof(RuntimeTypeHandle),
                    _ => typeof(object)
                },
                instruction
            );
            return;
        }

        else if(instruction.IsLdarg() || instruction.IsLdarga())
        {
            Push(parameters[instruction.ArgumentIndex()], instruction);
            return;
        }

        else if(instruction.IsStarg())
        {
            Pop();
            return;
        }

        else if(instruction.IsLdloc())
        {
            if(instruction.operand is LocalBuilder local)
            {
                Push(local.LocalType, instruction);
                return;
            }

            var localIndex = instruction.LocalIndex();
            var matchingLocal = locals.FirstOrDefault(local => local.LocalIndex == localIndex);
            if(matchingLocal is not null)
            {
                Push(matchingLocal.LocalType, instruction);
                return;
            }

            //Note: this shouldn't happen but keeping it just in case
            Push(typeof(object), instruction);
            return;
        }

        else if(instruction.IsStloc())
        {
            Pop();
            return;
        }

        else if(instruction.opcode == OpCodes.Isinst || instruction.opcode == OpCodes.Castclass)
        {
            Pop();
            Push((Type) instruction.operand, instruction);
            return;
        }
        
        else if(instruction.opcode == OpCodes.Ldftn || instruction.opcode == OpCodes.Ldvirtftn)
        {
            Push(typeof(Action), instruction); //Not entirely acurate but good enough for now
            return;
        }

        else if(instruction.operand is MethodBase method)
        {
            var paramCount = method.GetParameters().Length;
            if(!method.IsStatic && method is not ConstructorInfo) paramCount++;
            Pop(paramCount);

            if(method is ConstructorInfo constructorInfo)
            {
                Push(constructorInfo.DeclaringType!, instruction);
                return;
            }

            if(method is MethodInfo methodInfo && instruction.Calls(methodInfo))
            {
                if(methodInfo.ReturnType != typeof(void))
                {
                    Push(methodInfo.ReturnType, instruction);
                }
                return;
            }
        }

        else if(instruction.operand is FieldInfo field)
        {
            if(instruction.LoadsField(field) || instruction.LoadsField(field, true))
            {
                if(!field.IsStatic) Pop();
                Push(field.FieldType, instruction);
                return;
            }

            if(instruction.StoresField(field))
            {
                Pop(field.IsStatic ? 1 : 2);
                return;
            }
        }
        
        else if(MathCodes.Contains(instruction.opcode))
        {
            Pop(2);
            
            //Note: this is not always correct, but not really relevant in this case.
            Push(typeof(int), instruction);
            return;
        }
        
        else if (instruction.LoadsConstant())
        {
            //Note: this is not always correct, but not really relevant in this case.
            Push(instruction.opcode == OpCodes.Ldstr ? typeof(string) : typeof(int), instruction);
            return;
        }

        else if(instruction.opcode == OpCodes.Newarr)
        {
            Pop();
            Push(((Type) instruction.operand).MakeArrayType(), instruction);
            return;
        }

        else if (StelemCodes.Contains(instruction.opcode))
        {
            Pop(3);
            return;
        }
        
        else if (StindCodes.Contains(instruction.opcode))
        {
            Pop(2);
            return;
        }

        else if(LdelemCodes.Contains(instruction.opcode))
        {
            Pop();
            var type = Stack.Peek().Type.GetElementType() ?? typeof(object);
            Pop();
            Push(type, instruction);
            return;
        }

        else if (TrueFalseBranchCodes.Contains(instruction.opcode))
        {
            Pop();
            return;
        }
        else if (ComparitiveBranchCodes.Contains(instruction.opcode))
        {
            Pop(2);
            return;
        }

        else if(instruction.opcode == OpCodes.Ldlen)
        {
            Pop();
            Push(typeof(int), instruction);
            return;
        }
        else if(instruction.opcode == OpCodes.Initobj)
        {
            Pop();
            return;
        }
        else if(instruction.opcode == OpCodes.Initblk)
        {
            Pop(3);
            return;
        }

        else if(instruction.opcode == OpCodes.Switch)
        {
            Pop();
            return;
        }
        else if(instruction.opcode == OpCodes.Throw)
        {
            Pop();
            return;
        }

        else if(instruction.opcode == OpCodes.Mkrefany)
        {
            Pop();
            Push(typeof(TypedReference), instruction);
            return;
        }


        else if(instruction.opcode == OpCodes.Leave || instruction.opcode == OpCodes.Leave_S || instruction.opcode == OpCodes.Endfinally) return; // For try blocks, might need more work
        
        else if(DoNothingCodes.Contains(instruction.opcode) || UnconditionalJumpCodes.Contains(instruction.opcode) || ConvOpcodes.Contains(instruction.opcode) || LdindCodes.Contains(instruction.opcode)) return;
        
        throw new NotSupportedException($"Instruction '{instruction}' is not supported by stack emulator");
    }

    private void Push(Type returnType, CodeInstruction instruction)
    {
        var entry = new StackEntry
        {
            Instruction = instruction,
            Type = returnType
        };

        if (shouldTrace(this, entry))
        {
            Traces.Add(entry, new Trace
            {
                Origin = entry
            });
        }
        else foreach(var trace in Traces.Values)
        {
            if(trace.Usage.Contains(instruction) && shouldTraceAdvance(this, entry, trace))
            {
                trace.Path.Add(entry);
                Traces.Add(entry, trace);
                break;
            }
        }

        Stack.Push(entry);
    }

    private void Pop(int count = 1)
    {
        while (count-- > 0)
        {
            var removed = Stack.Pop();

            if(Traces.TryGetValue(removed, out var trace))
            {
                trace.Usage.Add(Matcher.Instruction);
            }
        }
    }
}