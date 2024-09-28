# Dependency Graph Generator for C# - Simplifying Test Setup

## Project Goal
The primary goal of this Dependency Graph Generator is to significantly reduce the time and effort required for setting up unit tests in C# projects. By automatically analyzing and mocking complex dependency structures, it aims to make the testing proccess easier, and make developers focus on writing meaningful tests rather than wrestling with dependency setup.

## Overview
This project implements an intelligent Dependency Graph Generator in C#. It analyzes a class's dependencies, constructs a comprehensive graph representation, and automatically generates mocked instances of all dependencies.

## Key Features

### Automated Dependency Analysis
- **Intelligent Categorization**: The generator automatically categorizes dependencies into leaf nodes and parent nodes based on their characteristics.
- **Recursive Analysis**: Handles nested dependencies, creating a complete graph of the entire dependency structure.

### Smart Mocking
- **Automatic Mock Generation**: Uses the Moq framework to automatically create mocked instances of all dependencies.
- **Contextual Mocking**: Generates mocks that are aware of their place in the dependency hierarchy.

### Graph Construction
- **Leaf Nodes**: Identified for:
  - Interfaces
  - Classes with parameterless constructors
  - Classes with only primitive type parameters in their constructors
- **Node Children**: Created for classes with at least one non-primitive dependency.

### Efficient Traversal
- **Depth-First Approach**: Traverses the graph from the deepest left-most child, and making sure that all dependencies are properly initialized before being used by their parents.

## Benefits for Testing
- **Reduced Setup Time**: Eliminates the need to manually mock and set up complex dependency chains.
- **Increased Test Coverage**: Makes it easier to test classes with numerous or deeply nested dependencies.
- **Improved Test Maintainability**: Changes in class dependencies are automatically reflected in the generated mocks, reducing the need to update test setups manually.

## Usage Example
### We want to test the SystemOrchestrator class.
```csharp

public interface IDataProcessor { }
public interface ILogger { }
public interface INetworkManager { }
public interface ISecurityProvider { }

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

```
### After running the Dependency Salvation we will automatically get
```csharp

_mockIDataProcessor0 = new Mock<IDataProcessor>();
_mockILogger1 = new Mock<ILogger>();
_mockINetworkManager2 = new Mock<INetworkManager>();
_mockISecurityProvider3 = new Mock<ISecurityProvider>();
_mockConfigurationManager4 = new Mock<ConfigurationManager>();
_emptyListOfString_5 = new List<String>();
_mockDataHandler6 = new Mock<DataHandler>(_emptyListOfString_5);
_emptyListOfApplicationCore_7 = new List<ApplicationCore>();
systemOrchestrator = new SystemOrchestrator(_mockIDataProcessor0.Object, _mockILogger1.Object, _mockINetworkManager2.Object, _mockISecurityProvider3.Object, _mockConfigurationManager4.Object, _mockDataHandler6.Object, _emptyListOfApplicationCore_7);

```

## Getting Started
1. clone the repo.
2. Enjoy.

## Contribution
Please Contact me.

## License
[Haat Delivery]

## Contact
[Razi Mograbi] - [razimograbi44@gmail.com]

Project Link: [https://github.com/yourusername/your-repo-name](https://github.com/yourusername/your-repo-name)
