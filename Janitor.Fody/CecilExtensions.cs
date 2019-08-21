using System;
using System.Collections.Generic;
using System.Linq;
using Fody;
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
        var attribute = attributes.SingleOrDefault(x => x.AttributeType.FullName == "Janitor.SkipWeaving");
        if (attribute != null)
        {
            attributes.Remove(attribute);
        }
    }

    public static bool MethodExists(this TypeDefinition typeDefinition, string method)
    {
        return typeDefinition.Methods.Any(x => x.Name == method);
    }

    public static bool FieldExists(this TypeDefinition typeDefinition, string field)
    {
        return typeDefinition.Fields.Any(x => x.Name == field);
    }

    public static bool IsClass(this TypeDefinition x)
    {
        return x.BaseType != null &&
               !x.IsEnum &&
               !x.IsInterface;
    }

    public static bool IsIDisposable(this TypeReference typeRef)
    {
        if (typeRef.IsArray)
        {
            return false;
        }
        if (typeRef.IsGenericParameter)
        {
            var genericParameter = (GenericParameter)typeRef;
            return genericParameter.Constraints.Any(c => c.ConstraintType.IsIDisposable());
        }
        var type = typeRef.Resolve();
        if (type.Interfaces.Any(i => i.InterfaceType.FullName.Equals("System.IDisposable")))
        {
            return true;
        }
        if (type.FullName.Equals("System.IDisposable"))
        {
            return true;
        }
        return type.BaseType != null &&
               type.BaseType.IsIDisposable();
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

        var parameters = methodReference.Parameters;
        for (var index = 0; index < parameters.Count; index++)
        {
            var parameterDefinition = parameters[index];
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
        if (value == null)
        {
            return false;
        }

        if (value.CustomAttributes
            .Select(x => x.AttributeType)
            .Any(a => a.Name == "CompilerGeneratedAttribute" ||
                      a.Name == "GeneratedCodeAttribute"))
        {
            return true;
        }

        return IsGeneratedCode((value as TypeDefinition)?.DeclaringType);
    }

    public static bool IsEmptyOrNotImplemented(this MethodDefinition method)
    {
        var instructions = method.Body.Instructions
            .Where(i => i.OpCode != OpCodes.Nop &&
                        i.OpCode != OpCodes.Ret).ToList();

        if (instructions.Count == 0)
        {
            return true;
        }

        if (instructions.Count != 2 ||
            instructions[0].OpCode != OpCodes.Newobj ||
            instructions[1].OpCode != OpCodes.Throw)
        {
            return false;
        }

        var ctor = (MethodReference)instructions[0].Operand;
        if (ctor.DeclaringType.FullName == "System.NotImplementedException")
        {
            return true;
        }

        return false;
    }

    public static FieldReference GetGeneric(this FieldDefinition definition)
    {
        if (!definition.DeclaringType.HasGenericParameters)
        {
            return definition;
        }
        var declaringType = new GenericInstanceType(definition.DeclaringType);
        foreach (var parameter in definition.DeclaringType.GenericParameters)
        {
            declaringType.GenericArguments.Add(parameter);
        }
        return new FieldReference(definition.Name, definition.FieldType, declaringType);
    }

    public static MethodReference GetGeneric(this MethodReference reference)
    {
        if (!reference.DeclaringType.HasGenericParameters)
        {
            return reference;
        }
        var declaringType = new GenericInstanceType(reference.DeclaringType);
        foreach (var parameter in reference.DeclaringType.GenericParameters)
        {
            declaringType.GenericArguments.Add(parameter);
        }

        var methodReference = new MethodReference(reference.Name, reference.MethodReturnType.ReturnType, declaringType)
        {
            HasThis = reference.HasThis
        };
        foreach (var parameterDefinition in reference.Parameters)
        {
            methodReference.Parameters.Add(parameterDefinition);
        }
        return methodReference;
    }

    public static void HideLineFromDebugger(SequencePoint seqPoint)
    {
        if (seqPoint?.Document == null)
        {
            return;
        }

        // This tells the debugger to ignore and step through
        // all the following instructions to the next instruction
        // with a valid SequencePoint. That way IL can be hidden from
        // the Debugger. See
        // http://blogs.msdn.com/b/abhinaba/archive/2005/10/10/479016.aspx
        seqPoint.StartLine = 0xfeefee;
        seqPoint.EndLine = 0xfeefee;
    }

    public static OpCode GetCallingConvention(this MethodReference method)
    {
        if (method.Resolve().IsVirtual)
        {
            return OpCodes.Callvirt;
        }
        return OpCodes.Call;
    }

    public static MethodReference MakeGeneric(this MethodReference method, params TypeReference[] args)
    {
        if (args.Length == 0)
        {
            return method;
        }

        if (method.GenericParameters.Count != args.Length)
        {
            throw new ArgumentException("Invalid number of generic type arguments supplied");
        }

        var genericTypeRef = new GenericInstanceMethod(method);
        foreach (var arg in args)
        {
            genericTypeRef.GenericArguments.Add(arg);
        }

        return genericTypeRef;
    }

    public static TypeReference GetDisposable(this TypeReference reference)
    {
        if (reference.IsGenericParameter)
        {
            var genericParameter = (GenericParameter)reference;
            return genericParameter.Constraints.First(c => c.ConstraintType.IsIDisposable()).ConstraintType;
        }

        return reference;
    }
}