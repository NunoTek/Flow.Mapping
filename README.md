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

## Requirements

- .NET 8.0 or higher
- Dependencies:
  - Microsoft.Extensions.Logging.Abstractions
  - Newtonsoft.Json
  - System.Linq
  - UTF.Unknown
  - XPath2

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Author

Nuno ARAUJO 