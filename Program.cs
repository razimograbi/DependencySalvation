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
        var instance = Activator.CreateInstance(type);
        var variableName = GenerateVariableName(type);
        objectToVariableName[instance] = variableName;

        sb.AppendLine($"{variableName} = new Mock<{type.Name}>();");
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

        // Generate the variable name
        var variableName = GenerateVariableName(type, isRootNode);
        objectToVariableName[instance] = variableName;

        // Generate parameter string representations
        var parameterStrings = parameters
            .Select(p => objectToVariableName.TryGetValue(p, out var name) ? $"{name}.Object" : GetParameterString(p))
            .ToArray();

        // Append the appropriate instantiation string
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
        if (parameter == null) return "null";
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
        var graph = builder.BuildDependencyGraph(typeof(MainApplication));
        Console.WriteLine("Constructing dependencies:");
        var result = builder.ConstructDependencies(graph);
    }
}

// Example classes for testing


public interface ICustomerService
{
    void AddCustomer(string name);
    string GetCustomerDetails(int customerId);
}

public interface IOrderService
{
    void PlaceOrder(int orderId, string product);
    string GetOrderStatus(int orderId);
}

public interface INotificationService
{
    void SendNotification(string message);
    string GetNotificationStatus(int notificationId);
}


public class PaymentProcessor
{
    private int _transactionNumber;
    private char _currency;
    private decimal _amount;
    private readonly INotificationService _notificationService;

    public PaymentProcessor(int transactionNumber, char currency, decimal amount, INotificationService notificationService)
    {
        _transactionNumber = transactionNumber;
        _currency = currency;
        _amount = amount;
        _notificationService = notificationService;
    }

    public void ProcessPayment()
    {
        // Payment logic
        _notificationService.SendNotification($"Payment of {_amount} {_currency} processed for transaction {_transactionNumber}");
    }
}

public class UserManager
{
    private string _userName;

    public UserManager(string userName)
    {
        _userName = userName;
    }

    public void UpdateUserName(string newUserName)
    {
        _userName = newUserName;
    }

    public string GetUserName()
    {
        return _userName;
    }
}

public class EcommerceSystem
{
    private readonly ICustomerService _customerService;
    private readonly IOrderService _orderService;
    private readonly PaymentProcessor _paymentProcessor;

    public EcommerceSystem(ICustomerService customerService, IOrderService orderService, PaymentProcessor paymentProcessor)
    {
        _customerService = customerService;
        _orderService = orderService;
        _paymentProcessor = paymentProcessor;
    }

    public void ProcessNewOrder(int customerId, int orderId, string product, decimal paymentAmount)
    {
        _customerService.AddCustomer($"Customer {customerId}");
        _orderService.PlaceOrder(orderId, product);
        _paymentProcessor.ProcessPayment();
    }
}

public class EmptyUtility
{
    public void DoNothing()
    {
        // Literally does nothing.
    }
}

public class MainApplication
{
    private readonly ICustomerService _customerService;
    private readonly IOrderService _orderService;
    private readonly INotificationService _notificationService;
    private readonly PaymentProcessor _paymentProcessor;
    private readonly UserManager _userManager;
    private readonly EcommerceSystem _ecommerceSystem;
    private readonly EmptyUtility _emptyUtility;

    // Constructor with six dependencies
    public MainApplication(ICustomerService customerService, IOrderService orderService, INotificationService notificationService,
                           PaymentProcessor paymentProcessor, UserManager userManager,
                           EcommerceSystem ecommerceSystem, EmptyUtility emptyUtility)
    {
        _customerService = customerService;
        _orderService = orderService;
        _notificationService = notificationService;
        _paymentProcessor = paymentProcessor;
        _userManager = userManager;
        _ecommerceSystem = ecommerceSystem;
        _emptyUtility = emptyUtility;
    }

    public void Start()
    {
        _customerService.AddCustomer("John Doe");
        _orderService.PlaceOrder(1, "Laptop");
        _paymentProcessor.ProcessPayment();
        _emptyUtility.DoNothing();
    }
}
