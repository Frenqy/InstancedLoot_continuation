using Mono.Cecil;
using Mono.Cecil.Cil;

// A deliberately bounded verifier for the complete installed CreateCommandCube body.
// Unknown instructions fail closed: extending the game method requires an explicit review.
internal static class CommandCubeStack
{
    internal static void Verify(MethodDefinition method)
    {
        if (method.Body.HasExceptionHandlers) throw new InvalidOperationException("Command cube exception regions are not covered.");
        var states = new Dictionary<Instruction, string[]>();
        var pending = new Queue<(Instruction Instruction, string[] Stack)>();
        pending.Enqueue((method.Body.Instructions[0], Array.Empty<string>()));
        while (pending.Count != 0)
        {
            var (instruction, incoming) = pending.Dequeue();
            if (states.TryGetValue(instruction, out var existing))
            {
                if (!existing.SequenceEqual(incoming))
                    throw new InvalidOperationException($"Command cube stack merge mismatch at {instruction}: [{string.Join(",", existing)}] vs [{string.Join(",", incoming)}].");
                continue;
            }
            states[instruction] = incoming;
            var stack = incoming.ToList();
            string Pop()
            {
                if (stack.Count == 0) throw new InvalidOperationException($"Command cube stack underflow at {instruction}.");
                string value = stack[^1];
                stack.RemoveAt(stack.Count - 1);
                return value;
            }
            void Consume(string expected)
            {
                string actual = Pop();
                if (actual != expected)
                    throw new InvalidOperationException($"Command cube stack type mismatch at {instruction}: expected {expected}, received {actual}.");
            }
            switch (instruction.OpCode.Code)
            {
                case Code.Nop: break;
                case Code.Ldarg_0: case Code.Ldarg_1: case Code.Ldarg_2: case Code.Ldarg_3:
                {
                    int index = instruction.OpCode.Code - Code.Ldarg_0;
                    stack.Add(method.HasThis && index == 0 ? Type(method.DeclaringType) : Type(method.Parameters[index - (method.HasThis ? 1 : 0)].ParameterType));
                    break;
                }
                case Code.Ldarg: case Code.Ldarg_S:
                    stack.Add(Type(((ParameterDefinition)instruction.Operand).ParameterType)); break;
                case Code.Ldc_I4: case Code.Ldc_I4_S: case Code.Ldc_I4_M1: case Code.Ldc_I4_0: case Code.Ldc_I4_1:
                case Code.Ldc_I4_2: case Code.Ldc_I4_3: case Code.Ldc_I4_4: case Code.Ldc_I4_5: case Code.Ldc_I4_6:
                case Code.Ldc_I4_7: case Code.Ldc_I4_8:
                    stack.Add("System.Int32"); break;
                case Code.Ldsfld:
                    stack.Add(Type(((FieldReference)instruction.Operand).FieldType)); break;
                case Code.Ldfld: case Code.Ldflda:
                {
                    var field = (FieldReference)instruction.Operand;
                    string owner = Pop();
                    if (owner.TrimEnd('&') != Type(field.DeclaringType))
                        throw new InvalidOperationException($"Command cube stack field owner mismatch at {instruction}.");
                    stack.Add(Type(field.FieldType) + (instruction.OpCode.Code == Code.Ldflda ? "&" : ""));
                    break;
                }
                case Code.Stfld:
                {
                    var field = (FieldReference)instruction.Operand;
                    Consume(Type(field.FieldType));
                    if (Pop().TrimEnd('&') != Type(field.DeclaringType))
                        throw new InvalidOperationException($"Command cube stack field owner mismatch at {instruction}.");
                    break;
                }
                case Code.Dup:
                {
                    string value = Pop(); stack.Add(value); stack.Add(value); break;
                }
                case Code.Pop: Pop(); break;
                case Code.Ldloc_0: case Code.Ldloc_1: case Code.Ldloc_2: case Code.Ldloc_3:
                    stack.Add(Type(method.Body.Variables[instruction.OpCode.Code - Code.Ldloc_0].VariableType)); break;
                case Code.Ldloc: case Code.Ldloc_S:
                    stack.Add(Type(((VariableDefinition)instruction.Operand).VariableType)); break;
                case Code.Stloc_0: case Code.Stloc_1: case Code.Stloc_2: case Code.Stloc_3:
                    Consume(Type(method.Body.Variables[instruction.OpCode.Code - Code.Stloc_0].VariableType)); break;
                case Code.Stloc: case Code.Stloc_S:
                    Consume(Type(((VariableDefinition)instruction.Operand).VariableType)); break;
                case Code.Call: case Code.Callvirt: case Code.Newobj:
                {
                    var called = (MethodReference)instruction.Operand;
                    for (int i = called.Parameters.Count - 1; i >= 0; i--) Consume(Type(called.Parameters[i].ParameterType, called));
                    if (called.HasThis && instruction.OpCode.Code != Code.Newobj)
                    {
                        string receiver = Pop();
                        if (receiver.TrimEnd('&') != Type(called.DeclaringType))
                            throw new InvalidOperationException($"Command cube stack receiver mismatch at {instruction}: {receiver}.");
                    }
                    if (instruction.OpCode.Code == Code.Newobj) stack.Add(Type(called.DeclaringType));
                    else if (called.ReturnType.MetadataType != MetadataType.Void) stack.Add(Type(called.ReturnType, called));
                    break;
                }
                case Code.Brfalse: case Code.Brfalse_S: case Code.Brtrue: case Code.Brtrue_S: case Code.Switch:
                    Consume("System.Int32"); break;
                case Code.Br: case Code.Br_S: break;
                case Code.Ret:
                    if (method.ReturnType.MetadataType != MetadataType.Void) Consume(Type(method.ReturnType));
                    if (stack.Count != 0) throw new InvalidOperationException("Command cube stack is not empty at ret.");
                    break;
                default: throw new InvalidOperationException($"Command cube stack verifier does not cover {instruction}.");
            }
            foreach (var target in CommandCubeRegression.Targets(instruction)) pending.Enqueue((target, stack.ToArray()));
            if (instruction.OpCode.FlowControl != FlowControl.Branch && instruction.OpCode.FlowControl != FlowControl.Return)
            {
                if (instruction.Next == null) throw new InvalidOperationException("Command cube control flow falls off the method.");
                pending.Enqueue((instruction.Next, stack.ToArray()));
            }
        }
        if (states.Count != method.Body.Instructions.Count)
            throw new InvalidOperationException("Command cube contains unverified unreachable instructions.");
    }

    private static string Type(TypeReference type, MethodReference called = null)
    {
        if (type is GenericParameter parameter && called != null)
        {
            if (parameter.Type == GenericParameterType.Method && called is GenericInstanceMethod genericMethod)
                return Type(genericMethod.GenericArguments[parameter.Position]);
            if (parameter.Type == GenericParameterType.Type && called.DeclaringType is GenericInstanceType genericType)
                return Type(genericType.GenericArguments[parameter.Position]);
        }
        if (type is GenericInstanceType generic)
            return generic.ElementType.FullName + "<" + string.Join(",", generic.GenericArguments.Select(t => Type(t))) + ">";
        if (type is ByReferenceType reference) return Type(reference.ElementType, called) + "&";
        return type.MetadataType == MetadataType.Boolean ? "System.Int32" : type.FullName;
    }
}
