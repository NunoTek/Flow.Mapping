namespace Flow.Mapping;

public class MapOptions
{
    public MapOptions() => IgnoreProperties = new List<KeyValuePair<string, string>>();

    public List<KeyValuePair<string, string>> IgnoreProperties { get; set; }
}
