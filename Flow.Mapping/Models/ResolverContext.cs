using System.Xml;

namespace Flow.Mapping.Models;

public class ResolverContext
{
    // Node of the entity
    public XmlNode ParentNode { get; set; }

    // Lien pour les mappings en cas de source multiple pour une même destination
    public string SourceName { get; set; }

    /// <summary>
    /// Value types: NodeList, Node or String
    /// </summary>
    public object Value { get; set; }

    public object Entity { get; set; }

    public string PropertyName { get; set; }

    public Type DestinationType { get; set; }

    public MapOptions MapOptions { get; set; }

    public List<ResolverArgument> ValueResolverArguments { get; set; }

    public string GetArgValue(string argName) => ValueResolverArguments?.Where(a => a.Name == argName).Select(a => a.Value).FirstOrDefault();
}
