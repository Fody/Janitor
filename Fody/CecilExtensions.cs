using System.Collections.Generic;
using System.Linq;
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

    public static void ThrowIfMethodExists(this TypeDefinition typeDefinition, string method)
    {
        if (typeDefinition.Methods.Any(x => x.Name == method))
        {
            var message = string.Format("Type `{0}` contains a `{1}` method. Either remove this method or add a `[Janitor.SkipWeaving]` attribute to the type.", typeDefinition.FullName, method);
            throw new WeavingException(message);
        }
    }
    public static void ThrowIfFieldExists(this TypeDefinition typeDefinition, string field)
    {
        if (typeDefinition.Fields.Any(x => x.Name == field))
        {
            var message = string.Format("Type `{0}` contains a `{1}` field. Either remove this field or add a `[Janitor.SkipWeaving]` attribute to the type.", typeDefinition.FullName, field);
            throw new WeavingException(message);
        }
    }
    public static bool IsClass(this TypeDefinition x)
    {
        return (x.BaseType != null) && !x.IsEnum && !x.IsInterface;
    }

    public static bool IsIDisposable(this TypeReference typeRef)
    {
        var type = typeRef.Resolve();
        return (type.Interfaces.Any(i => i.FullName.Equals("System.IDisposable"))
                || (type.BaseType != null && type.BaseType.IsIDisposable()));
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
        throw new WeavingException(string.Format("Could not find '{0}' on '{1}'", name, typeReference.Name) );
    }

    public static string GetName(this FieldDefinition field)
    {
        return string.Format("{0}.{1}", field.DeclaringType.FullName, field.Name);
    }

    public static bool IsMatch(this MethodReference methodReference,string name, params string[] paramTypes)
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
}