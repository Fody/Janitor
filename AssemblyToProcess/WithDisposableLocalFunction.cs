using System;
using System.Collections.Generic;
using System.Linq;

public class WithDisposableLocalFunction
{
    public IEnumerable<string> MethodWithLocalFunction()
    {
        return ContainedTypes(typeof(IDictionary<int, string>)).ToList();

        IEnumerable<string> ContainedTypes(Type type)
        {
            yield return type.FullName;

            if (type.IsGenericType)
            {
                foreach (var containedType in
                    type.GenericTypeArguments.SelectMany(ContainedTypes))
                {
                    yield return containedType;
                }
            }
        }
    }
}