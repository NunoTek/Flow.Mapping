namespace Flow.Mapping.Models;


[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class FlowMapping : Attribute
{
    public int FluxId { get; set; }

    public string MapFrom { get; set; } // XPATH

    public MapFromTypes MapType { get; set; } = MapFromTypes.XPath;

    public string EntityName { get; set; }
    public string PropertyName { get; set; }

    public int MappingOrder { get; set; }

    public string SourceName { get; set; } // Nom absolut du noeud xml

    public string PreConditionXPath { get; set; } // Must be a boolean expression xpath

    public ValueResolverTypes ValueResolver { get; set; } // TODO: peremetre la personalization

    public string ValueResolverArguments { get; set; }

    public string NullSubstitute { get; set; }
}
