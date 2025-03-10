using Flow.Mapping.Abstractions;
using Flow.Mapping.Errors;
using Flow.Mapping.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using UtfUnknown;
using Wmhelp.XPath2;

namespace Flow.Mapping;

public class FlowMapper<TRoot> : IFlowMapper<TRoot>, IFlowInternalMapper
    where TRoot : FlowRoot, new()
{
    private List<FlowMapping> _mappings;
    private Dictionary<ValueResolverTypes, IFlowValueResolver> _resolvers;
    private readonly FlowTranscodifier _transcodifier;

    private readonly IFlowResourceLoader _loader;
    private readonly ILogger<FlowMapper<TRoot>> _logger;


    public FlowMapper(IFlowResourceLoader loader, FlowTranscodifier flowTranscodifer = null, ILogger<FlowMapper<TRoot>> logger = null)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(IFlowResourceLoader));
        _transcodifier = flowTranscodifer ?? new FlowTranscodifier(_loader);
        _logger = logger;
    }


    public static XmlDocument LoadDoc(string fileName, string baseTag = null)
    {
        var doc = new XmlDocument();

        DetectionResult fileEncoding = CharsetDetector.DetectFromFile(fileName);
        bool isReedable = fileEncoding.Detected != null;

        var encodingDetected = new List<Encoding>();

        if (isReedable)
            encodingDetected.AddRange(fileEncoding.Details.OrderBy(x => x.Confidence).Select(x => x.Encoding));

        encodingDetected.AddRange(new[] {
            null, // Will check for unicode chars, then use unicode encoding else use the default encoding
            Encoding.UTF8,
        });


        bool isJsonFile = fileName.ToLower().EndsWith("json");
        if (isJsonFile && baseTag == null)
        {
            var attr = typeof(TRoot).GetProperties().FirstOrDefault()?.CustomAttributes?.FirstOrDefault(a => a.AttributeType == typeof(FlowMapping));
            if (attr != null)
            {
                var sourceName = attr.NamedArguments.FirstOrDefault(a => a.MemberName == nameof(FlowMapping.SourceName));
                baseTag = sourceName.TypedValue.Value.ToString();
            }
        }

        foreach (var encoding in encodingDetected)
        {
            try
            {
                if (isJsonFile)
                {
                    var data = FlowHelper.ConvertJsonToXml(fileName, baseTag, encoding);
                    doc.LoadXml(data);
                }
                else
                {
                    var xml = FlowHelper.SanitizeContent(fileName, encoding);
                    doc.LoadXml(xml);
                }

                isReedable = true;
                break;
            }
            catch (Exception ex)
            {

            }
        }

        if (!isReedable)
            throw new XmlException(FlowErrors.RootElementMissingXml);

        if (string.IsNullOrEmpty(doc.OuterXml))
            throw new XmlException(FlowErrors.EmptyXml);

        return doc;
    }

    private async Task InitializeAsync(int fluxId, CancellationToken cancelToken)
    {
        _mappings = await _loader.LoadMappingsAsync(fluxId, cancelToken);
        _resolvers = await _loader.LoadResolversAsync(fluxId, cancelToken);

        await _transcodifier.InitializeAsync(fluxId, cancelToken);
    }


    public async Task<TRoot> MapAsync(int fluxId, string fileName, CancellationToken cancelToken = default)
    {
        await InitializeAsync(fluxId, cancelToken);

        var baseTag = _mappings.OrderBy(m => m.SourceName.Length).FirstOrDefault()?.SourceName;

        var doc = LoadDoc(fileName, baseTag);

        var rootEntity = new TRoot();

        MapEntity(doc.DocumentElement, rootEntity);

        await MapTranscodificationsAsync(fluxId, cancelToken);

        return rootEntity;
    }

    private async Task MapTranscodificationsAsync(int fluxId, CancellationToken cancelToken)
    {
        try
        {
            await _transcodifier.ReportFoundTranscodificationsAsync(fluxId, cancelToken);
        }
        catch (Exception ex)
        {
            LogMappingException("FlowMapper Error: while mapping transcodifications : {error}", ex);
        }
    }

    public string GetNodePath(XmlNode node, XmlNode parentToStopAt = null)
    {
        var sb = new StringBuilder(node.Name);

        XmlNode parentNode = node.ParentNode;

        while (parentNode != null && !(parentNode is XmlDocument) && parentNode != parentToStopAt)
        {
            sb.Insert(0, "/");
            sb.Insert(0, parentNode.Name);

            parentNode = parentNode.ParentNode;
        }

        return sb.ToString();
    }

    public void EnsureInstantiateList(object obj, PropertyInfo property)
    {
        var list = property.GetValue(obj);

        if (list == null)
        {
            list = Activator.CreateInstance(property.PropertyType);

            property.SetValue(obj, list);
        }
    }

    public string MapValue(FlowMapping map, string value = null)
    {
        if (map.MapType == MapFromTypes.Value)
        {
            value = map.MapFrom;
        }

        if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(map.NullSubstitute))
        {
            value = map.NullSubstitute;
        }

        if (GetTranscodedValue(map.EntityName, map.PropertyName, value, out string newValue))
        {
            value = newValue;
        }

        return value;
    }

    public void MapPrimitiveProp(XmlNode parentNode, FlowMapping map, object entity, PropertyInfo property, MapOptions opts = null)
    {
        if (property == null)
            return;

        string value = null;

        if (map.MapType == MapFromTypes.XPath)
        {
            var xpath = ResolveXPathArgs(parentNode, map.MapFrom);
            value = GetNodeString(parentNode, xpath);
        }

        value = MapValue(map, value);

        if (!string.IsNullOrEmpty(value))
        {
            SetPropertyValue(entity, property, value);
        }
    }

    public void MapList(XmlNode parentNode, FlowMapping mapping, object entity, PropertyInfo property, MapOptions opts = null)
    {
        if (property == null)
            return;

        EnsureInstantiateList(entity, property);

        var xpath = ResolveXPathArgs(parentNode, mapping.MapFrom);
        var nodeList = parentNode.XPath2SelectNodes(xpath);

        if (nodeList.Count == 0)
            return;

        var itemType = property.PropertyType.GetGenericArguments()[0];
        var listValue = property.GetValue(entity);

        foreach (XmlNode childNode in nodeList)
        {
            if (!childNode.HasChildNodes)
                continue;

            var newItem = Activator.CreateInstance(itemType);

            property.PropertyType.GetMethod("Add").Invoke(listValue, new[] { newItem });

            MapEntity(childNode, newItem, opts);
        }
    }

    public void MapEntity(XmlNode parentNode, object entity, MapOptions opts = null)
    {
        var entityType = entity.GetType();
        var mappings = GetMappings(entityType, GetNodePath(parentNode));

        foreach (var map in mappings)
        {
            try
            {
                var property = entityType.GetProperty(map.PropertyName);

                if (property == null)
                    continue;

                if (map.PropertyName == "DureeDemembrement" || map.PropertyName == "ValeurEconomiqueNP")
                {

                }

                var propType = property.PropertyType;

                // Ignore specified properties
                if (opts?.IgnoreProperties.Any(p => p.Key == map.EntityName && p.Value == map.PropertyName) == true)
                    continue;

                // Execute the xpath pre condition
                if (!string.IsNullOrEmpty(map.PreConditionXPath))
                {
                    var navigation = parentNode.CreateNavigator();
                    bool? boolResult = navigation.XPath2Evaluate(map.PreConditionXPath) as bool?;

                    if (boolResult == false)
                        continue;
                }

                bool isTypeList = propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>);

                // Look for the resolver set with the mapping
                if (map.ValueResolver != ValueResolverTypes.None)
                {
                    if (isTypeList)
                    {
                        EnsureInstantiateList(entity, property);
                    }

                    object value = null;

                    if (map.MapType == MapFromTypes.XPath)
                    {
                        var xpath = ResolveXPathArgs(parentNode, map.MapFrom);

                        value = GetNodeValue(parentNode, xpath);
                    }

                    var converter = _resolvers[map.ValueResolver];
                    var context = new ResolverContext()
                    {
                        DestinationType = property.PropertyType,
                        Entity = entity,
                        PropertyName = property.Name,
                        ParentNode = parentNode,
                        SourceName = map.SourceName,
                        Value = value,
                        MapOptions = opts,
                        ValueResolverArguments = map.ValueResolverArguments != null
                            // TODO: https://github.com/dotnet/runtime/issues/32291
                            ? JsonConvert.DeserializeObject<List<ResolverArgument>>(map.ValueResolverArguments)
                            : null
                    };

                    var resolvedValue = converter.Resolve(context, this);

                    if (resolvedValue == null)
                        continue;

                    if (isTypeList)
                    {
                        var listValue = property.GetValue(entity);

                        if (resolvedValue is IList)
                        {
                            property.PropertyType.GetMethod("AddRange").Invoke(listValue, new object[] { resolvedValue });
                        }
                        else
                        {
                            property.PropertyType.GetMethod("Add").Invoke(listValue, new object[] { resolvedValue });
                        }
                    }
                    else
                    {
                        if (resolvedValue is string resolvedStringValue)
                        {
                            resolvedStringValue = MapValue(map, resolvedStringValue);

                            if (!string.IsNullOrEmpty(resolvedStringValue))
                            {
                                SetPropertyValue(entity, property, resolvedStringValue);
                            }
                        }
                        else
                        {
                            property.SetValue(entity, resolvedValue);
                        }
                    }

                    continue;
                }


                if (isTypeList)
                {
                    MapList(parentNode, map, entity, property, opts);
                }
                else // Primitive types, object are not supported yet
                {
                    MapPrimitiveProp(parentNode, map, entity, property, opts);
                }
            }
            catch (Exception ex)
            {
                LogMappingException(map, ex);
            }
        }
    }

    public bool GetTranscodedValue(string entityName, string propertyName, string value, out string newValue)
        => _transcodifier.GetTranscodedValue(entityName, propertyName, value, out newValue);

    // ConvertValue
    public void SetPropertyValue(object obj, PropertyInfo property, string value)
    {
        var propType = property.PropertyType;

        // Prevent error on Convert.ChangeType when the type is nullable
        var typeUnboxed = Nullable.GetUnderlyingType(propType) ?? propType;

        object objValue = null;

        try
        {
            if (typeUnboxed == typeof(string))
            {
                objValue = value;
            }
            else if (typeUnboxed == typeof(DateTime))
            {
                DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime dateVal);

                objValue = dateVal;
            }
            else if (typeUnboxed == typeof(DateTimeOffset))
            {
                DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTimeOffset dateVal);

                objValue = dateVal;
            }
            else if (typeUnboxed == typeof(int))
            {
                if (int.TryParse(value, out int intVal))
                    objValue = intVal;
            }
            else if (typeUnboxed == typeof(decimal))
            {
                decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decVal);

                objValue = decVal;
            }
            else if (typeUnboxed.IsEnum)
            {
                Enum.TryParse(typeUnboxed, value, true, out objValue);
            }
            else
            {
                objValue = Convert.ChangeType(value, typeUnboxed, CultureInfo.InvariantCulture);
            }

            property.SetValue(obj, objValue);

            _transcodifier.RequestTranscodification(property.DeclaringType.Name, property.Name, value, objValue?.ToString());
        }
        catch
        {
            _transcodifier.RequestTranscodification(property.DeclaringType.Name, property.Name, value);
            throw;
        }
    }

    public IEnumerable<FlowMapping> GetMappings(string entityName, string propertyName, string sourceName = null)
    {
        return _mappings
            .Where(m => m.EntityName == entityName && m.PropertyName == propertyName && (sourceName == null || m.SourceName == sourceName))
            .OrderBy(m => m.MappingOrder);
    }

    public IEnumerable<FlowMapping> GetMappings(Type classType, string sourceName = null)
    {
        return _mappings
            .Where(m => m.EntityName == classType.Name && (sourceName == null || m.SourceName == sourceName))
            .OrderBy(m => m.MappingOrder);
    }

    // Exemple de cas : //cs_lots/cs_lot[cs_lotprogramid='{cs_programid}']
    // La méthode va évaluer {cs_programid}
    public string ResolveXPathArgs(XmlNode parentNode, string xpath)
    {
        var argsMatches = Regex.Matches(xpath, @"\{(.*?)\}");

        // Resolve all arguments in the xpath
        foreach (Match match in argsMatches)
        {
            var argName = match.Value;
            var argValue = parentNode.XPath2SelectSingleNode(match.Groups[1].Value)?.InnerText;

            xpath = xpath.Replace(argName, argValue);
        }

        return xpath;
    }

    public string GetNodeString(XmlNode parentNode, string xpath)
    {
        var xpathExpr = parentNode.CreateNavigator().Compile(xpath);

        if (xpathExpr.ReturnType == XPathResultType.NodeSet)
        {
            return parentNode.XPath2SelectSingleNode(xpath)?.InnerText;
        }
        else
        {
            // If the xpath expr is not a node-set then evaluate it
            return parentNode.CreateNavigator().XPath2Evaluate(xpath)?.ToString();
        }
    }

    public object GetNodeValue(XmlNode parentNode, string xpath)
    {
        var xpathExpr = parentNode.CreateNavigator().Compile(xpath);

        if (xpathExpr.ReturnType == XPathResultType.NodeSet)
        {
            return parentNode.XPath2SelectNodes(xpath);
        }
        else
        {
            // If the xpath expr is not a node-set then evaluate it
            return parentNode.CreateNavigator().XPath2Evaluate(xpath);
        }
    }


    public virtual void LogMappingInfo(string message, params object?[]? args)
    {
        if (_logger != null)
        {
            _logger.LogInformation(message, args);
            return;
        }

        Console.WriteLine(message, args);
    }

    public virtual void LogMappingException(string format, Exception ex)
    {
        string errorMessage = ex.InnerException?.Message ?? ex.Message;
        if (_logger != null)
        {
            _logger.LogError(ex, format, errorMessage);
            return;
        }

        Console.WriteLine(format, errorMessage);
    }

    public virtual void LogMappingException(FlowMapping map, Exception ex)
    {
        string errorMessage = ex.InnerException?.Message ?? ex.Message;
        if (_logger != null)
        {
            _logger.LogError(ex, "FlowMapper Error: while mapping flow on: {entity}.{property}:{from}\r\n{error}", map.EntityName, map.PropertyName, map.MapFrom, errorMessage);
            return;
        }

        Console.WriteLine("{0}.{1}:{2}\r\n{3}", map.EntityName, map.PropertyName, map.MapFrom, errorMessage);
    }

}
