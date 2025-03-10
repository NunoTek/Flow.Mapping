using System.Reflection;
using System.Xml;
using Flow.Mapping.Models;

namespace Flow.Mapping.Abstractions;

public interface IFlowInternalMapper
{
    string MapValue(FlowMapping map, string value = null);

    void MapPrimitiveProp(XmlNode parentNode, FlowMapping map, object entity, PropertyInfo property, MapOptions opts = null);

    void MapList(XmlNode parentNode, FlowMapping mapping, object entity, PropertyInfo property, MapOptions opts = null);

    void MapEntity(XmlNode parentNode, object entity, MapOptions opts = null);

    bool GetTranscodedValue(string entityName, string propertyName, string value, out string newValue);

    IEnumerable<FlowMapping> GetMappings(string entityName, string propertyName, string sourceName = null);

    string GetNodePath(XmlNode node, XmlNode parentToStopAt = null);
}
