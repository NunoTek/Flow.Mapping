# Flow.Mapping

[![NuGet](https://img.shields.io/nuget/v/Flow.Mapping.svg)](https://www.nuget.org/packages/Flow.Mapping)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

Flow.Mapping is a powerful .NET library designed to facilitate flexible and efficient mapping between XML/JSON data sources and .NET objects. It provides a robust framework for handling complex data transformations with support for custom value resolvers and data transcodification.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage](#usage)
- [FlowMapping Attribute Reference](#flowmapping-attribute-reference)
- [Built-in Value Resolvers](#built-in-value-resolvers)
- [Advanced Features](#advanced-features)
- [Usage Examples](#usage-examples)
- [Logging Configuration](#logging-configuration)
- [Why Newtonsoft.Json?](#why-newtonsoftjson)
- [Requirements and Compatibility](#requirements-and-compatibility)
- [Contributing](#contributing)
- [License](#license)

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
- **Conditional Mapping**: Use XPath preconditions to control when mappings are applied
- **Null Handling**: Built-in null substitution support for default values

## Installation

```bash
dotnet add package Flow.Mapping
```

Or via the .NET CLI:

```bash
dotnet add package Flow.Mapping --version 1.0.0
```

## Quick Start

Here's a minimal example to get you started in under 5 minutes:

```csharp
using Flow.Mapping;
using Flow.Mapping.Models;

// 1. Define your model
public class ProductRoot : FlowRoot
{
    [FlowMapping(MapFrom = "//product/name")]
    public string Name { get; set; }

    [FlowMapping(MapFrom = "//product/price", ValueResolver = ValueResolverTypes.FrenchCurrencyFormat)]
    public decimal Price { get; set; }
}

// 2. Create a simple loader (or implement full IFlowResourceLoader)
public class ProductLoader : IFlowResourceLoader
{
    public Task<List<FlowMapping>> LoadMappingsAsync(int fluxId, CancellationToken cancelToken)
        => Task.FromResult(new List<FlowMapping>());
    
    // ... implement other interface methods
}

// 3. Map your data
var mapper = new FlowMapper<ProductRoot>(new ProductLoader());
var result = await mapper.MapAsync(fluxId: 1, fileName: "products.xml");
Console.WriteLine($"Product: {result.Root.Name} - {result.Root.Price:C}");
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

## FlowMapping Attribute Reference

The `FlowMapping` attribute supports the following properties:

| Property | Type | Description |
|----------|------|-------------|
| `MapFrom` | `string` | XPath expression to extract the value from the source document |
| `MapType` | `MapFromTypes` | Mapping type: `XPath` (default) or `Value` for direct assignment |
| `SourceName` | `string` | Absolute node name in XML (used for JSON root detection) |
| `EntityName` | `string` | Target entity name for the mapping |
| `PropertyName` | `string` | Target property name |
| `MappingOrder` | `int` | Order of execution when multiple mappings exist |
| `PreConditionXPath` | `string` | XPath boolean expression that must be true for mapping to apply |
| `ValueResolver` | `ValueResolverTypes` | Built-in resolver to transform the value |
| `ValueResolverArguments` | `string` | Arguments passed to the value resolver |
| `NullSubstitute` | `string` | Default value when the source is null or empty |
| `FluxId` | `int` | Identifier for grouping mappings by data flow |

### Example with all options

```csharp
[FlowMapping(
    MapFrom = "//order/total",
    MapType = MapFromTypes.XPath,
    PreConditionXPath = "//order/status = 'confirmed'",
    ValueResolver = ValueResolverTypes.FrenchCurrencyFormat,
    NullSubstitute = "0",
    MappingOrder = 1
)]
public decimal OrderTotal { get; set; }
```

## Built-in Value Resolvers

Flow.Mapping includes the following built-in resolvers:

| Resolver | Description | Example Input | Example Output |
|----------|-------------|---------------|----------------|
| `StringBool` | Converts string to boolean | `"yes"`, `"1"`, `"true"` | `true` |
| `StringDateFormat` | Parses date strings | `"15/09/2022"` | `DateTime` |
| `FrenchNumberFormat` | Parses French decimal format | `"1 234,56"` | `1234.56` |
| `FrenchCurrencyFormat` | Parses French currency | `"1.234,56 €"` | `1234.56m` |
| `DistinctByValue` | Returns distinct items from a list | `[A, B, A, C]` | `[A, B, C]` |
| `AverageValue` | Calculates average of numeric values | `[10, 20, 30]` | `20` |
| `ExtractFileNameFromUri` | Extracts filename from URI | `"http://example.com/file.pdf"` | `"file.pdf"` |
| `UnescapeHtml` | Decodes HTML entities | `"&amp;lt;div&amp;gt;"` | `"<div>"` |
| `HtmlStyleSanitizer` | Removes inline HTML styles | `"<p style='...'>"` | `"<p>"` |
| `MapIfDecimalValueGreaterThanZero` | Maps only if value > 0 | `"5.00"` | `5.00m` or `null` |
| `NumberToListOfMappedElement` | Converts number to list | `3` | `[item, item, item]` |

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
    public Task<List<Transcodification>> LoadTranscodificationsAsync(int fluxId, CancellationToken cancelToken)
    {
        // Return your transcodification rules
    }
}
```

## IFlowResourceLoader Interface

The `IFlowResourceLoader` interface is the central configuration point for Flow.Mapping. Here's the complete interface:

```csharp
public interface IFlowResourceLoader
{
    // List all types that can be mapped for a given flux
    Task<List<Type>> ListMappableClassTypesAsync(int fluxId, CancellationToken cancelToken);

    // List properties that support transcodification
    Task<List<(string EntityName, string PropertyName)>> ListTranscodableAsync(int fluxId, CancellationToken cancelToken);

    // Load mapping configurations
    Task<List<FlowMapping>> LoadMappingsAsync(int fluxId, CancellationToken cancelToken);

    // Load transcodification rules
    Task<List<Transcodification>> LoadTranscodificationsAsync(int fluxId, CancellationToken cancelToken);

    // Load custom value resolvers
    Task<Dictionary<ValueResolverTypes, IFlowValueResolver>> LoadResolversAsync(int fluxId, CancellationToken cancelToken);

    // Persist new transcodification rules (for auto-learning scenarios)
    Task InsertTranscodificationAsync(int fluxId, List<Transcodification> transcodifications, CancellationToken cancelToken);
}
```

### Complete Implementation Example

```csharp
public class DatabaseResourceLoader : IFlowResourceLoader
{
    private readonly IDbConnection _db;

    public DatabaseResourceLoader(IDbConnection db) => _db = db;

    public async Task<List<FlowMapping>> LoadMappingsAsync(int fluxId, CancellationToken cancelToken)
    {
        // Load mappings from database
        return await _db.QueryAsync<FlowMapping>(
            "SELECT * FROM FlowMappings WHERE FluxId = @FluxId",
            new { FluxId = fluxId });
    }

    public async Task<List<Transcodification>> LoadTranscodificationsAsync(int fluxId, CancellationToken cancelToken)
    {
        return await _db.QueryAsync<Transcodification>(
            "SELECT * FROM Transcodifications WHERE FluxId = @FluxId",
            new { FluxId = fluxId });
    }

    public Task<Dictionary<ValueResolverTypes, IFlowValueResolver>> LoadResolversAsync(int fluxId, CancellationToken cancelToken)
    {
        // Return default resolvers plus any custom ones
        var resolvers = new Dictionary<ValueResolverTypes, IFlowValueResolver>(FlowHelper.DefaultResolvers);
        // Add custom resolvers here if needed
        return Task.FromResult(resolvers);
    }

    public Task<List<Type>> ListMappableClassTypesAsync(int fluxId, CancellationToken cancelToken)
        => Task.FromResult(new List<Type> { typeof(ProductRoot), typeof(OrderRoot) });

    public Task<List<(string EntityName, string PropertyName)>> ListTranscodableAsync(int fluxId, CancellationToken cancelToken)
        => Task.FromResult(new List<(string, string)> { ("Product", "Category"), ("Order", "Status") });

    public async Task InsertTranscodificationAsync(int fluxId, List<Transcodification> transcodifications, CancellationToken cancelToken)
    {
        foreach (var trans in transcodifications)
        {
            await _db.ExecuteAsync(
                "INSERT INTO Transcodifications (FluxId, Source, Target, Entity, Field) VALUES (@FluxId, @Source, @Target, @Entity, @Field)",
                new { FluxId = fluxId, trans.Source, trans.Target, trans.Entity, trans.Field });
        }
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

// Register in your loader
public Task<Dictionary<ValueResolverTypes, IFlowValueResolver>> LoadResolversAsync(int fluxId, CancellationToken cancelToken)
{
    var resolvers = new Dictionary<ValueResolverTypes, IFlowValueResolver>(FlowHelper.DefaultResolvers);
    // Custom resolvers would need to extend the ValueResolverTypes enum or use a different approach
    return Task.FromResult(resolvers);
}
```

### Example 5 — Using ResolverContext for advanced scenarios

The `ResolverContext` provides rich context for custom resolvers:

```csharp
public class ConditionalResolver : IFlowValueResolver
{
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        // Access the parent XML node for additional context
        var parentNode = context.ParentNode;
        
        // Get the current entity being mapped
        var entity = context.Entity;
        
        // Access resolver arguments passed via ValueResolverArguments
        var format = context.GetArgValue("format") ?? "default";
        
        // Check destination type for proper conversion
        if (context.DestinationType == typeof(decimal))
        {
            return decimal.Parse(context.Value?.ToString() ?? "0");
        }
        
        return context.Value;
    }
}
```

### Example 6 — Conditional mapping with PreConditionXPath

```csharp
public class OrderRoot : FlowRoot
{
    // Only map if the order status is 'confirmed'
    [FlowMapping(
        MapFrom = "//order/total",
        PreConditionXPath = "//order/status = 'confirmed'",
        ValueResolver = ValueResolverTypes.FrenchCurrencyFormat
    )]
    public decimal? ConfirmedTotal { get; set; }

    // Only map if quantity is greater than 0
    [FlowMapping(
        MapFrom = "//order/quantity",
        PreConditionXPath = "//order/quantity > 0"
    )]
    public int Quantity { get; set; }
}
```

### Example 7 — Transcodification rules

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

### Example 8 — Error handling

```csharp
public async Task SafeMapAsync(string fileName)
{
    var loader = new SimpleLoader();
    var mapper = new FlowMapper<PersonRoot>(loader, logger: _logger);

    try
    {
        var result = await mapper.MapAsync(fluxId: 1, fileName: fileName);
        
        if (result.Root != null)
        {
            // Process successfully mapped data
            Console.WriteLine($"Mapped: {result.Root.Name}");
        }
    }
    catch (FileNotFoundException ex)
    {
        _logger.LogError(ex, "Source file not found: {FileName}", fileName);
    }
    catch (XmlException ex)
    {
        _logger.LogError(ex, "Invalid XML format in: {FileName}", fileName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Mapping failed for: {FileName}", fileName);
        throw;
    }
}
```

### Example 9 — Working with nested entities

```xml
<catalog>
  <product id="1">
    <name>Widget</name>
    <variants>
      <variant sku="W-001">Red</variant>
      <variant sku="W-002">Blue</variant>
    </variants>
  </product>
</catalog>
```

```csharp
public class CatalogRoot : FlowRoot
{
    [FlowMapping(SourceName = "catalog")]
    public string RootName { get; set; }

    [FlowMapping(MapFrom = "//product/name")]
    public string ProductName { get; set; }

    [FlowMapping(MapFrom = "//product/variants/variant")]
    public List<string> VariantNames { get; set; }

    [FlowMapping(MapFrom = "//product/variants/variant/@sku")]
    public List<string> VariantSkus { get; set; }
}
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

## Error Messages

Flow.Mapping provides clear error messages for common issues:

| Error | Cause | Solution |
|-------|-------|----------|
| `Empty content.` | Source file is empty | Verify file has content |
| `Root Element is missing.` | XML lacks root element | Ensure valid XML structure |

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Nuno ARAUJO**

- GitHub: [@NunoTek](https://github.com/NunoTek)
- Repository: [Flow.Mapping](https://github.com/NunoTek/Flow.Mapping)