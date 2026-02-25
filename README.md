# Dependency Graph Generator for C# - Simplifying Test Setup

## ⚠️ Warning

This program isn't ready for production use. Please exercise caution when using it, as it may contain untested features or bugs. **The production-ready version is private** and not available in this repository.



## Project Goal
The primary goal of this Dependency Graph Generator is to significantly reduce the time and effort required for setting up unit tests in C# projects. Expically Huge projects with many Dependency injections, By automatically analyzing and mocking complex dependency structures, I aim to make the testing proccess easier, and make developers focus on writing meaningful tests rather than Struggling with dependency setup.

## Overview
This project implements an intelligent Way to automattically create Mocked Instances Of the class using Reflection.


## Usage Example 1 (Production Example XUNIT)
```csharp
public class OrderCancellationSimulationServiceTests
{
    private readonly AutomaticallyMockedClass _mocker;
    private readonly OrderCancellationSimulationService _sut;

    public OrderCancellationSimulationServiceTests()
    {
        _mocker = AutoClassMocker<OrderCancellationSimulationService>.DeepMockDependencies();
        _sut = _mocker.GetSutInstance<OrderCancellationSimulationService>();
    }

    [Fact]
    public async Task SimulateCancellationAsync_Should..._when..(){
             var cancellationMock = _mocker.GetMockedDependency<IExampleRepo>();

       cancellationMock
           .Setup(x => x.GetMethodExampleAsync(It.IsAny<int>()))
           .ReturnsAsync(new CustomExample { IsActive = true, IsDefault = false });

      var request = new ExampleRequest(price : 200);
    
      var res = await _sut.SimulateCancellationAsync(request);
       Assert.NotNull(res);
       Assert.NotNull(res.InnerObject);
       Assert.Equal(250, res.InnerObject.FinalPriceExample);
    }

}

```

## Usage Example 2
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

Project Link: [https://github.com/razimograbi/DependencySalvation/tree/master](https://github.com/razimograbi/DependencySalvation/tree/master)
