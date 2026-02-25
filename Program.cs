using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Moq;
using TestingGraph;


public class Program
{
    public static void Main()
    {
        var result = AutoClassMocker<SystemOrchestrator>.DeepMockDependencies();
        Console.WriteLine(result.GetSutInstance<SystemOrchestrator>());
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
