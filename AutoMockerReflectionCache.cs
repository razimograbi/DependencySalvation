using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace TestingGraph
{
    internal static class AutoMockerReflectionCache
    {
        public static readonly ConcurrentDictionary<Type, ConstructorInfo> ConstructorCache = new();
        public static readonly ConcurrentDictionary<Type, Type[]> ConstructorParamsCache = new();
        public static readonly ConcurrentDictionary<Type, Type> MockTypeCache = new();
        public static readonly ConcurrentDictionary<Type, PropertyInfo> MockDefaultValuePropCache = new();
        public static readonly ConcurrentDictionary<Type, PropertyInfo> MockObjectPropCache = new();
    }
}