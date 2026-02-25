using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;

namespace TestingGraph
{
    public sealed class DependencyMetadata
    {
        /// <summary>
        /// The actual object instance that was passed into the constructor.
        /// If the dependency is an interface, this is the '.Object' proxy from Moq.
        /// If it is a concrete class, this is the real class instance.
        /// </summary>
        public object Implementation { get; init; }

        /// <summary>
        /// The Moq wrapper for this dependency.
        /// This is used to perform .Setup() and .Verify() in unit tests.
        /// This will be null if the dependency was a concrete class.
        /// </summary>
        public object MockWrapper { get; init; }


        public bool IsMocked => MockWrapper != null;
    }

    public sealed class AutomaticallyMockedClass
    {
        private readonly Dictionary<Type, DependencyMetadata> _dependenciesMocks;
        private readonly Type _originalServiceUnderTestType;
        public AutomaticallyMockedClass(Dictionary<Type, DependencyMetadata> dependenciesMocks, Type originalServiceUnderTestType)
        {
            _dependenciesMocks = dependenciesMocks;
            _originalServiceUnderTestType = originalServiceUnderTestType;
        }

        public Mock<T> GetMockedDependency<T>() where T : class => (Mock<T>)_dependenciesMocks[typeof(T)].MockWrapper;
        public T GetSutInstance<T>() where T : class => (T)_dependenciesMocks[_originalServiceUnderTestType].Implementation;
    }

    public static class AutoClassMocker<T> where T : class
    {
        private const int DictionarySizeLimit = 100;

        public static AutomaticallyMockedClass DeepMockDependencies()
        {
            var result = new Dictionary<Type, DependencyMetadata>();

            object CreateMockedObjects(Type type)
            {
                if (result.Count > DictionarySizeLimit)
                {
                    throw new Exception("Max Dictionary Size reached");
                }
                if (result.TryGetValue(type, out var foundObject))
                {
                    return foundObject.Implementation;
                }

                if (type.IsInterface)
                {
                    var moqInterface = MockInterface(type);
                    result.Add(type, moqInterface);
                    return moqInterface.Implementation;
                }

                var constructor = AutoMockerReflectionCache.ConstructorCache.GetOrAdd(type, t =>
                {
                    var ctor = t.GetConstructors().FirstOrDefault()
                               ?? t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();

                    if (ctor == null)
                    {
                        throw new Exception($"Failed to find public, private, or protected constructor for {t.Name}");
                    }
                    return ctor;
                });


                var parameterTypes = AutoMockerReflectionCache.ConstructorParamsCache.GetOrAdd(
                    type,
                    _ => constructor.GetParameters().Select(p => p.ParameterType).ToArray()
                );

                var constructorParams = new List<object>(parameterTypes.Length);
                foreach (var paramType in parameterTypes)
                {
                    constructorParams.Add(CreateMockedObjects(paramType));
                }

                var classInstance = constructor.Invoke(constructorParams.ToArray());
                if (classInstance == null)
                {
                    throw new Exception($"Failed to instantiate class {type.Name}");
                }

                result.Add(type, new DependencyMetadata{ Implementation = classInstance });

                return classInstance;
            }



            try
            {
                CreateMockedObjects(typeof(T));
                return new AutomaticallyMockedClass(result, typeof(T));
            }
            catch
            {
                ClearAllCachesInFailure();
                throw;
            }
        }

        private static DependencyMetadata MockInterface(Type type)
        {
            var mockType = AutoMockerReflectionCache.MockTypeCache.GetOrAdd(type, t => typeof(Mock<>).MakeGenericType(t));

            var mockInstance = Activator.CreateInstance(mockType);
            if (mockInstance == null)
            {
                throw new Exception($"Mock instance is null for the type {type.Name}");
            }

            // Mocked interfaces will now return en empty list instead of null when calling an async method
            // for example Task<List<Order>> GetOrdersAsync() returns an empty list instead of null.

            var defaultValueProp = AutoMockerReflectionCache.MockDefaultValuePropCache.GetOrAdd(mockType, mt => mt.GetProperty("DefaultValue"));
            if (defaultValueProp != null)
            {
                defaultValueProp.SetValue(mockInstance, Moq.DefaultValue.Empty);
            }

            var propertyInfo = AutoMockerReflectionCache.MockObjectPropCache.GetOrAdd(mockType, mt => mt.GetProperty("Object", type));
            if (propertyInfo == null)
            {
                throw new Exception($"Could not find the 'Object' property on Mock<{type.Name}.");
            }

            var proxyObject = propertyInfo.GetValue(mockInstance);
            if (proxyObject == null)
            {
                throw new Exception($"Mocked object is null for the type {type.Name}");
            }

            return new DependencyMetadata { Implementation = proxyObject, MockWrapper = mockInstance };
        }

        private static void ClearAllCachesInFailure()
        {
            AutoMockerReflectionCache.ConstructorCache.Clear();
            AutoMockerReflectionCache.ConstructorParamsCache.Clear();
            AutoMockerReflectionCache.MockTypeCache.Clear();
            AutoMockerReflectionCache.MockDefaultValuePropCache.Clear();
            AutoMockerReflectionCache.MockObjectPropCache.Clear();
        }

    }

}