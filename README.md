# Flow.Mapping

Flow.Mapping is a powerful .NET library designed to facilitate flexible and efficient mapping between XML/JSON data sources and .NET objects. It provides a robust framework for handling complex data transformations with support for custom value resolvers and data transcodification.

## Features

- **XML and JSON Support**: Automatically detects and handles both XML and JSON input files
- **Flexible Mapping**: Map data using XPath expressions or direct value assignments
- **Encoding Detection**: Automatic file encoding detection and handling
- **Value Resolvers**: Built-in resolvers for common data transformation scenarios:
  - String to Boolean conversion
  - Date format parsing
  - Number format handling (including French number formats)
  - Currency format parsing
  - HTML content handling
  - File name extraction from URIs
  - List operations (distinct, average, etc.)
- **Transcodification**: Support for value transformation and mapping between different representations
- **Extensible Architecture**: Easy to add custom value resolvers for specific needs

## Installation

```bash
dotnet add package Flow.Mapping
```

## Usage

### Basic Setup

1. Create your model class inheriting from `FlowRoot`:

```csharp
public class MyRoot : FlowRoot
{
    [FlowMapping(SourceName = "rootElement")]
    public string Property1 { get; set; }
    
    [FlowMapping(MapFrom = "//path/to/element")]
    public int Property2 { get; set; }
}
```

2. Implement the `IFlowResourceLoader` interface for your mapping configuration:

```csharp
public class MyResourceLoader : IFlowResourceLoader
{
    public async Task<List<FlowMapping>> LoadMappingsAsync(int fluxId, CancellationToken cancelToken)
    {
        // Return your mapping configurations
    }
    
    // Implement other interface methods...
}
```

3. Use the mapper:

```csharp
var loader = new MyResourceLoader();
var mapper = new FlowMapper<MyRoot>(loader);
var result = await mapper.MapAsync(fluxId: 1, fileName: "data.xml");
```

### Using Value Resolvers

The library includes several built-in value resolvers for common scenarios:

```csharp
[FlowMapping(MapFrom = "//date", ValueResolver = ValueResolverTypes.StringDateFormat)]
public DateTime Date { get; set; }

[FlowMapping(MapFrom = "//price", ValueResolver = ValueResolverTypes.FrenchCurrencyFormat)]
public decimal Price { get; set; }

[FlowMapping(MapFrom = "//items", ValueResolver = ValueResolverTypes.DistinctByValue)]
public List<Item> UniqueItems { get; set; }
```

### Mapping Options

You can customize mapping behavior using `MapOptions`:

```csharp
var options = new MapOptions
{
    IgnoreProperties = new List<KeyValuePair<string, string>>
    {
        new("EntityName", "PropertyToIgnore")
    }
};
```

## Advanced Features

### Custom Value Resolvers

Create custom value resolvers by implementing the `IFlowValueResolver` interface:

```csharp
public class CustomResolver : IFlowValueResolver
{
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        // Implement your custom resolution logic
    }
}
```

### Transcodification

The library supports value transcodification for complex mapping scenarios:

```csharp
public class MyResourceLoader : IFlowResourceLoader
{
    public async Task<List<Transcodification>> LoadTranscodificationsAsync(int fluxId, CancellationToken cancelToken)
    {
        // Return your transcodification rules
    }
}
```

## Usage Examples

Below are more detailed usage examples to help users integrate the library in common scenarios.

### Example 1 — Full end-to-end mapping (XML)

This example shows a minimal end-to-end mapping from an XML file to a .NET object, including a simple `IFlowResourceLoader` implementation.

`sample-data.xml`:

```xml
<root>
  <person>
    <name>Jane Doe</name>
    <age>29</age>
    <joined>2022-09-15</joined>
    <salary>1.234,56</salary>
  </person>
</root>
```

Model and mappings:

```csharp
public class PersonRoot : FlowRoot
{
    [FlowMapping(MapFrom = "//person/name")]
    public string Name { get; set; }

    [FlowMapping(MapFrom = "//person/age")]
    public int Age { get; set; }

    [FlowMapping(MapFrom = "//person/joined", ValueResolver = ValueResolverTypes.StringDateFormat)]
    public DateTime Joined { get; set; }

    [FlowMapping(MapFrom = "//person/salary", ValueResolver = ValueResolverTypes.FrenchCurrencyFormat)]
    public decimal Salary { get; set; }
}

public class SimpleLoader : IFlowResourceLoader
{
    public Task<List<FlowMapping>> LoadMappingsAsync(int fluxId, CancellationToken cancelToken)
    {
        var mappings = new List<FlowMapping>
        {
            new FlowMapping { PropertyName = "Name", MapFrom = "//person/name" },
            new FlowMapping { PropertyName = "Age", MapFrom = "//person/age" },
            new FlowMapping { PropertyName = "Joined", MapFrom = "//person/joined", ValueResolver = ValueResolverTypes.StringDateFormat },
            new FlowMapping { PropertyName = "Salary", MapFrom = "//person/salary", ValueResolver = ValueResolverTypes.FrenchCurrencyFormat }
        };

        return Task.FromResult(mappings);
    }

    // Minimal implementations for other interface methods omitted for brevity
}
```

Using the mapper:

```csharp
var loader = new SimpleLoader();
var mapper = new FlowMapper<PersonRoot>(loader);
var result = await mapper.MapAsync(fluxId: 1, fileName: "sample-data.xml");

// result.Root contains the populated PersonRoot instance
```

### Example 2 — JSON input and selective mapping

```json
{
  "items": [
    { "id": "a1", "qty": 2, "price": "12.50" },
    { "id": "b2", "qty": 1, "price": "7.99" }
  ]
}
```

```csharp
public class OrderRoot : FlowRoot
{
    [FlowMapping(MapFrom = "$.items[*].id")]
    public List<string> ItemIds { get; set; }

    [FlowMapping(MapFrom = "$.items[*].price", ValueResolver = ValueResolverTypes.StringToDecimal)]
    public List<decimal> Prices { get; set; }
}

// Loader returns mappings for the properties above
```

### Example 3 — Batch processing multiple files asynchronously

```csharp
public async Task ProcessFilesAsync(IEnumerable<string> files)
{
    var loader = new SimpleLoader();
    var mapper = new FlowMapper<PersonRoot>(loader);

    var tasks = files.Select(f => mapper.MapAsync(fluxId: 1, fileName: f));
    var results = await Task.WhenAll(tasks);

    foreach (var r in results)
    {
        // Handle r.Root
    }
}
```

### Example 4 — Custom value resolver

```csharp
public class UppercaseResolver : IFlowValueResolver
{
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        var value = context.Value as string;
        return value?.ToUpperInvariant();
    }
}

[FlowMapping(MapFrom = "//title", ValueResolver = typeof(UppercaseResolver))]
public string Title { get; set; }
```

### Example 5 — Transcodification rules

```csharp
// Example transcodification rule mapping external codes to internal values
public class MyTranscodificationsLoader : IFlowResourceLoader
{
    public Task<List<Transcodification>> LoadTranscodificationsAsync(int fluxId, CancellationToken cancelToken)
    {
        var list = new List<Transcodification>
        {
            new Transcodification { Source = "EXT_A", Target = "INT_1", Entity = "Product", Field = "Category" },
            new Transcodification { Source = "EXT_B", Target = "INT_2", Entity = "Product", Field = "Category" }
        };

        return Task.FromResult(list);
    }
}

// During mapping, the mapper will apply these rules when resolving the Category field
```

## Logging Configuration

Flow.Mapping supports `Microsoft.Extensions.Logging.Abstractions` for diagnostics and troubleshooting.

### Basic logging setup

```csharp
using Microsoft.Extensions.Logging;

// Create a logger factory (use your DI container in production)
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger<FlowMapper<PersonRoot>>();
var loader = new SimpleLoader();
var mapper = new FlowMapper<PersonRoot>(loader, logger: logger);

var result = await mapper.MapAsync(fluxId: 1, fileName: "data.xml");
```

### Logging with dependency injection (ASP.NET Core)

```csharp
public class MyService
{
    private readonly ILogger<FlowMapper<PersonRoot>> _logger;
    private readonly IFlowResourceLoader _loader;

    public MyService(ILogger<FlowMapper<PersonRoot>> logger, IFlowResourceLoader loader)
    {
        _logger = logger;
        _loader = loader;
    }

    public async Task<FlowResult<PersonRoot>> ProcessFileAsync(string fileName)
    {
        var mapper = new FlowMapper<PersonRoot>(_loader, logger: _logger);
        return await mapper.MapAsync(fluxId: 1, fileName: fileName);
    }
}
```

### Configuring log levels in appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Flow.Mapping": "Debug"
    }
  }
}
```

### Using Serilog

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/flow-mapping.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
var logger = loggerFactory.CreateLogger<FlowMapper<PersonRoot>>();

var mapper = new FlowMapper<PersonRoot>(loader, logger: logger);
```

The logger will capture important events such as:
- File encoding detection results
- Mapping errors and warnings
- Value resolver execution details
- Transcodification rule applications

## Why Newtonsoft.Json?

Flow.Mapping uses **Newtonsoft.Json** (Json.NET) for several important reasons:

- **JSON to XML Conversion**: The library leverages `JsonConvert.DeserializeXmlNode()` to convert JSON documents to XML for unified XPath processing. This method is a mature, battle-tested feature of Newtonsoft.Json that handles complex edge cases.
- **XPath-based Processing**: By converting both JSON and XML to a common XML representation, Flow.Mapping can use powerful XPath 2.0 queries (via the XPath2 library) for consistent data extraction regardless of source format.
- **Mature Ecosystem**: Newtonsoft.Json has extensive production usage and handles a wide variety of JSON formats, including non-standard or legacy formats that may appear in real-world data files.
- **Compatibility**: Many existing projects already use Newtonsoft.Json, making integration seamless.

While System.Text.Json is the modern default for .NET, it does not provide equivalent XML conversion capabilities, and migrating would require a complete architectural redesign of the mapping pipeline. For projects requiring System.Text.Json, consider using it at the application boundary and letting Flow.Mapping handle its internal JSON processing with Newtonsoft.Json.

## Requirements and Compatibility

### Target Frameworks

| Version | Target Framework | Status |
|---------|------------------|--------|
| 1.x | .NET 10.0 | ? Current |

### Supported Platforms

- **Windows**: ? Fully supported (x64, x86, ARM64)
- **Linux**: ? Fully supported (x64, ARM64)
- **macOS**: ? Fully supported (x64, ARM64)

### Dependencies

| Package | Version | Purpose |
|---------|---------|----------|
| Microsoft.Extensions.Logging.Abstractions | 10.0.0 | Diagnostic logging support |
| Newtonsoft.Json | 13.0.4 | JSON parsing and JSON-to-XML conversion |
| UTF.Unknown | 2.6.0 | Automatic file encoding detection |
| XPath2 | 1.1.5 | Advanced XPath 2.0 query support |

### Minimum Requirements

- **.NET 10.0** or higher
- **C# 13** language features (implicit usings)

### Known Limitations

- XML files must be well-formed (use the built-in sanitization for common issues like unescaped ampersands)
- Very large files (>100MB) may require additional memory tuning
- JSONPath expressions in JSON files are limited to the subset supported by Newtonsoft.Json's XML conversion

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Author

Nuno ARAUJO