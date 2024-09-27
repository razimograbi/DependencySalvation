using System;
using System.Linq;
using System.Reflection;
namespace TestingGraph
{
    static class TypeUtil
    {

        public static bool IsLeafDependency(Type type)
        {
            return type.IsInterface || HasEmptyConstructor(type) || HasOnlyPrimitiveConstructor(type);
        }

        public static bool HasEmptyConstructor(Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        public static ConstructorInfo GetPreferredConstructor(Type type)
        {
            var emptyConstructor = type.GetConstructor(Type.EmptyTypes);
            if (emptyConstructor != null)
            {
                return emptyConstructor;
            }
            return type.GetConstructors().OrderBy(c => c.GetParameters().Length).FirstOrDefault();
        }

        public static object GetDefaultValue(Type type)
        {
            if(type.IsEnum) return Enum.GetValues(type).GetValue(0); 
            if (type == typeof(string)) return string.Empty;
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null;
        }
        public static bool IsPrimitiveOrString(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type.IsValueType || type.IsEnum;
        }
        private static bool HasOnlyPrimitiveConstructor(Type type)
        {
            var constructors = type.GetConstructors();
            return constructors.Any(constructor =>
                constructor.GetParameters().All(p => IsPrimitiveOrString(p.ParameterType)));
        }



    }
}
