using System.Collections.Generic;
using System.Linq;
using Anotar.Custom;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

public static class CecilExtensions
{
    public static bool ContainsSkipWeaving(this IEnumerable<CustomAttribute> attributes)
    {
        return attributes.Any(x => x.AttributeType.FullName == "Janitor.SkipWeaving");
    }

    public static void RemoveSkipWeaving(this Collection<CustomAttribute> attributes)
    {
        var attribute = attributes.FirstOrDefault(x => x.AttributeType.FullName == "Janitor.SkipWeaving");
        if (attribute != null)
        {
            attributes.Remove(attribute);
        }
    }

    public static bool MethodExists(this TypeDefinition typeDefinition, string method)
    {
        if (typeDefinition.Methods.Any(x => x.Name == method))
        {
            LogTo.Error("Type `{0}` contains a `{1}` method. Either remove this method or add a `[Janitor.SkipWeaving]` attribute to the type.", typeDefinition.FullName, method);
            return true;
        }
        return false;
    }

    public static bool FieldExists(this TypeDefinition typeDefinition, string field)
    {
        if (typeDefinition.Fields.Any(x => x.Name == field))
        {
            LogTo.Error("Type `{0}` contains a `{1}` field. Either remove this field or add a `[Janitor.SkipWeaving]` attribute to the type.", typeDefinition.FullName, field);
            return true;
        }
        return false;
    }

    public static bool IsClass(this TypeDefinition x)
    {
        return (x.BaseType != null) && !x.IsEnum && !x.IsInterface;
    }

    public static bool IsIDisposable(this TypeReference typeRef)
    {
        if (typeRef.IsArray)
        {
            return false;
        }
        var type = typeRef.Resolve();
        if (type.Interfaces.Any(i => i.FullName.Equals("System.IDisposable")))
        {
            return true;
        }
        if (type.FullName.Equals("System.IDisposable"))
        {
            return true;
        }
        return type.BaseType != null && type.BaseType.IsIDisposable();
    }

    public static void InsertAtStart(this Collection<Instruction> collection, params Instruction[] instructions)
    {
        var indexOf = 0;
        foreach (var instruction in instructions)
        {
            collection.Insert(indexOf, instruction);
            indexOf++;
        }
    }

    public static void Add(this Collection<Instruction> collection, params Instruction[] instructions)
    {
        foreach (var instruction in instructions)
        {
            collection.Add(instruction);
        }
    }

    public static void Add(this Collection<Instruction> collection, IEnumerable<Instruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            collection.Add(instruction);
        }
    }

    public static MethodDefinition Find(this TypeDefinition typeReference, string name, params string[] paramTypes)
    {
        foreach (var method in typeReference.Methods)
        {
            if (method.IsMatch(name, paramTypes))
            {
                return method;
            }
        }
        throw new WeavingException($"Could not find '{name}' on '{typeReference.Name}'");
    }

    public static string GetName(this FieldDefinition field)
    {
        return $"{field.DeclaringType.FullName}.{field.Name}";
    }

    public static bool IsMatch(this MethodReference methodReference, string name, params string[] paramTypes)
    {
        if (methodReference.Parameters.Count != paramTypes.Length)
        {
            return false;
        }
        if (methodReference.Name != name)
        {
            return false;
        }
        for (var index = 0; index < methodReference.Parameters.Count; index++)
        {
            var parameterDefinition = methodReference.Parameters[index];
            var paramType = paramTypes[index];
            if (parameterDefinition.ParameterType.Name != paramType)
            {
                return false;
            }
        }
        return true;
    }

    public static bool IsGeneratedCode(this ICustomAttributeProvider value)
    {
        return value.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute" || a.AttributeType.Name == "GeneratedCodeAttribute");
    }

    public static bool IsEmptyOrNotImplemented(this MethodDefinition method)
    {
        var instructions = method.Body.Instructions.Where(i => i.OpCode != OpCodes.Nop && i.OpCode != OpCodes.Ret).ToList();

        if (instructions.Count == 0)
            return true;

        if (instructions.Count != 2 || instructions[0].OpCode != OpCodes.Newobj || instructions[1].OpCode != OpCodes.Throw)
            return false;

        var ctor = (MethodReference)instructions[0].Operand;
        if (ctor.DeclaringType.FullName == "System.NotImplementedException")
            return true;

        return false;
    }

    public static FieldReference GetGeneric(this FieldDefinition definition)
    {
        if (definition.DeclaringType.HasGenericParameters)
        {
            var declaringType = new GenericInstanceType(definition.DeclaringType);
            foreach (var parameter in definition.DeclaringType.GenericParameters)
            {
                declaringType.GenericArguments.Add(parameter);
            }
            return new FieldReference(definition.Name, definition.FieldType, declaringType);
        }

        return definition;
    }

    public static MethodReference GetGeneric(this MethodReference reference)
    {
        if (reference.DeclaringType.HasGenericParameters)
        {
            var declaringType = new GenericInstanceType(reference.DeclaringType);
            foreach (var parameter in reference.DeclaringType.GenericParameters)
            {
                declaringType.GenericArguments.Add(parameter);
            }
            var methodReference = new MethodReference(reference.Name, reference.MethodReturnType.ReturnType, declaringType);
            foreach (var parameterDefinition in reference.Parameters)
            {
                methodReference.Parameters.Add(parameterDefinition);
            }
            methodReference.HasThis = reference.HasThis;
            return methodReference;
        }

        return reference;
    }

    public static void HideLineFromDebugger(this Instruction i, SequencePoint seqPoint)
    {
        if (seqPoint == null)
            return;

        HideLineFromDebugger(i, seqPoint.Document);
    }

    public static void HideLineFromDebugger(this Instruction i, Document doc)
    {
        if (doc == null)
            return;

        // This tells the debugger to ignore and step through
        // all the following instructions to the next instruction
        // with a valid SequencePoint. That way IL can be hidden from
        // the Debugger. See
        // http://blogs.msdn.com/b/abhinaba/archive/2005/10/10/479016.aspx
        i.SequencePoint = new SequencePoint(doc)
        {
            StartLine = 0xfeefee,
            EndLine = 0xfeefee
        };
    }

    public static OpCode GetCallingConvention(this MethodReference method)
    {
        return method.Resolve().IsVirtual ? OpCodes.Callvirt : OpCodes.Call;
    }
}