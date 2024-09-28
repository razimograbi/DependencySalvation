using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Moq;
using TestingGraph;

public class DependencyNode
{
    public string ClassName { get; }
    public Type ClassType { get; }
    public List<DependencyNode> Children { get; } = new List<DependencyNode>();
    public bool IsLeaf { get; set; }
    public object Instance { get; set; }
    public bool IsRoot { get; }

    private static bool _addedRootNodeFlag = false;

    public DependencyNode(string className, Type classType, bool isLeaf = false)
    {
        ClassName = className;
        ClassType = classType;
        IsLeaf = isLeaf;
        if (!_addedRootNodeFlag)
        {
            IsRoot = true;
            _addedRootNodeFlag = true;
        }
        else
        {
            IsRoot = false;
        }
    }

    public void AddChild(DependencyNode child)
    {
        Children.Add(child);
    }
}


public class DependencyGraphBuilder
{
    private int mockCounter = 0;
    private Dictionary<object, string> objectToVariableName = new Dictionary<object, string>();

    public DependencyNode BuildDependencyGraph(Type type)
    {
        var rootNode = new DependencyNode(type.Name, type);
        var constructor = TypeUtil.GetPreferredConstructor(type);

        if (constructor == null)
        {
            rootNode.IsLeaf = true;
            return rootNode;
        }

        foreach (var parameter in constructor.GetParameters())
        {
            var paramType = parameter.ParameterType;
            if (TypeUtil.IsLeafDependency(paramType))
            {
                rootNode.AddChild(new DependencyNode(paramType.Name, paramType, true));
            }
            else
            {
                var childNode = BuildDependencyGraph(paramType);
                rootNode.AddChild(childNode);
            }
        }

        return rootNode;
    }

    public object ConstructDependencies(DependencyNode node)
    {
        StringBuilder sb = new StringBuilder();
        var result = ConstructDependenciesInternal(node, sb);
        Console.WriteLine(sb.ToString());
        return result;
    }

    private object ConstructDependenciesInternal(DependencyNode node, StringBuilder sb)
    {
        if (node.IsLeaf)
        {
            return CreateMockForLeaf(node.ClassType, sb);
        }

        var constructor = TypeUtil.GetPreferredConstructor(node.ClassType);
        var parameters = constructor.GetParameters();

        if (parameters.Length == 0)
        {
            return CreateInstanceWithEmptyConstructor(node.ClassType, sb);
        }

        var childInstances = new List<object>();
        foreach (var child in node.Children)
        {
            childInstances.Add(ConstructDependenciesInternal(child, sb));
        }

        return CreateInstance(node.ClassType, childInstances.ToArray(), sb, node.IsRoot);
    }


    private object CreateMockForLeaf(Type type, StringBuilder sb)
    {
        if (TypeUtil.IsPrimitiveOrString(type))
        {
            return TypeUtil.GetDefaultValue(type);  
        }
        if (type.IsInterface)
        {
            return CreateMockInstance(type, sb);
        }
        if (TypeUtil.HasEmptyConstructor(type))
        {
            return CreateInstanceWithEmptyConstructor(type, sb);
        }
        return CreateInstanceWithPrimitiveConstructor(type, sb);
    }


    private object CreateMockInstance(Type type, StringBuilder sb)
    {
        var mockType = typeof(Mock<>).MakeGenericType(type);
        var mockInstance = Activator.CreateInstance(mockType);

        var objectProperty = mockType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .FirstOrDefault(prop => prop.Name == "Object" && prop.PropertyType == type);

        if (objectProperty == null)
        {
            throw new InvalidOperationException($"Could not find 'Object' property for mock of type {type.Name}");
        }

        var mockObject = objectProperty.GetValue(mockInstance);
        var variableName = GenerateVariableName(type);
        objectToVariableName[mockObject] = variableName;

        sb.AppendLine($"{variableName} = new Mock<{type.Name}>();");
        return mockObject;
    }

    private object CreateInstanceWithEmptyConstructor(Type type, StringBuilder sb)
    {
        string variableName = "";
        object instance;

        if (type.IsGenericType && (typeof(IEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition()) ||
                                   type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
        {
            var genericArgumentType = type.GetGenericArguments()[0];
            variableName = $"_empty{type.Name.Substring(0, type.Name.Length - 2)}Of{genericArgumentType.Name}_{mockCounter++}";

            // Check for specific IEnumerable implementations
            if (type.GetGenericTypeDefinition() == typeof(List<>))
            {
                sb.AppendLine($"{variableName} = new List<{genericArgumentType.Name}>();");
                instance = Activator.CreateInstance(typeof(List<>).MakeGenericType(genericArgumentType));
            }
            else if (type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                sb.AppendLine($"{variableName} = new HashSet<{genericArgumentType.Name}>();");
                instance = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(genericArgumentType));
            }
            else if (type.GetGenericTypeDefinition() == typeof(Queue<>))
            {
                sb.AppendLine($"{variableName} = new Queue<{genericArgumentType.Name}>();");
                instance = Activator.CreateInstance(typeof(Queue<>).MakeGenericType(genericArgumentType));
            }
            else if (type.GetGenericTypeDefinition() == typeof(Stack<>))
            {
                sb.AppendLine($"{variableName} = new Stack<{genericArgumentType.Name}>();");
                instance = Activator.CreateInstance(typeof(Stack<>).MakeGenericType(genericArgumentType));
            }
            else
            {
                // For other IEnumerable implementations, try to create an instance directly
                sb.AppendLine($"{variableName} = new {type.Name}<{genericArgumentType.Name}>();");
                instance = Activator.CreateInstance(type);
            }
        }
        else
        {
            instance = Activator.CreateInstance(type);
            variableName = GenerateVariableName(type);
            sb.AppendLine($"{variableName} = new Mock<{type.Name}>();");
        }

        objectToVariableName[instance] = variableName;
        return instance;
    }

    private object CreateInstanceWithPrimitiveConstructor(Type type, StringBuilder sb)
    {
        var constructor = type.GetConstructors().First();
        var parameters = constructor.GetParameters()
            .Select(p => TypeUtil.GetDefaultValue(p.ParameterType))
            .ToArray();

        var instance = Activator.CreateInstance(type, parameters);
        var variableName = GenerateVariableName(type);
        objectToVariableName[instance] = variableName;

        var parameterStrings = parameters.Select(GetParameterString).ToArray();
        sb.AppendLine($"{variableName} = new Mock<{type.Name}>({string.Join(", ", parameterStrings)});");

        return instance;
    }

    private object CreateInstance(Type type, object[] parameters, StringBuilder sb, bool isRootNode = false)
    {
        var constructor = TypeUtil.GetPreferredConstructor(type);
        var instance = constructor.Invoke(parameters);

        var variableName = GenerateVariableName(type, isRootNode);
        objectToVariableName[instance] = variableName;

        var parameterStrings = parameters
                .Select(p => objectToVariableName.TryGetValue(p, out var name)
                    ? (name.StartsWith("_mock") ? $"{name}.Object" : name)
                    : GetParameterString(p))
                .ToArray();

        sb.AppendLine(CreateInstantiationString(type, variableName, parameterStrings, isRootNode));

        return instance;
    }

    private string GenerateVariableName(Type type, bool isRootNode=false)
    {
        if (isRootNode)
        {
            return $"{char.ToLower(type.Name[0])}{type.Name.Substring(1)}"; 
        }
        return $"_mock{type.Name}{mockCounter++}";
    }

    private string CreateInstantiationString(Type type, string variableName, string[] parameterStrings, bool isRootNode)
    {
        var joinedParams = string.Join(", ", parameterStrings);
        return isRootNode
            ? $"{variableName} = new {type.Name}({joinedParams});"
            : $"{variableName} = new Mock<{type.Name}>({joinedParams});";
    }

    private string GetParameterString(object parameter)
    {
        if (parameter is null) return "null";
        if (parameter is string) return $"\"{parameter}\"";
        if (parameter is char) return $"'{parameter}'";
        return parameter.ToString();
    }
}

public class Program
{
    public static void Main()
    {
        var builder = new DependencyGraphBuilder();
        var graph = builder.BuildDependencyGraph(typeof(SystemOrchestrator));
        Console.WriteLine("Constructing dependencies:");
        var result = builder.ConstructDependencies(graph);
    }
}

// Example classes for testing


// Interfaces
public interface IDataProcessor { }
public interface ILogger { }
public interface INetworkManager { }
public interface ISecurityProvider { }

// First class with two constructors
public class ConfigurationManager
{
    public ConfigurationManager() { }
    public ConfigurationManager(string configPath) { }
}

// Second class implementing one interface
public class DataHandler : IDataProcessor
{
    public DataHandler(List<string> dataItems) { }
}

// Third class with multiple dependencies
public class ApplicationCore
{
    public ApplicationCore(IDataProcessor dataProcessor, ILogger logger, ConfigurationManager config, DataHandler dataHandler, long number, HashSet<int> set) { }
}

// Final class incorporating all elements
public class SystemOrchestrator
{
    public SystemOrchestrator(
        IDataProcessor dataProcessor,
        ILogger logger,
        INetworkManager networkManager,
        ISecurityProvider securityProvider,
        ConfigurationManager configManager,
        DataHandler dataHandler,
        List<ApplicationCore> appCore 
    )
    { }
}
