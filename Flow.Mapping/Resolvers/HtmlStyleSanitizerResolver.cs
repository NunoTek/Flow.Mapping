using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using System.Text.RegularExpressions;
using System.Xml;

namespace Flow.Mapping.Resolvers;

public class HtmlStyleSanitizerResolver : IFlowValueResolver
{
    private static readonly Regex baliseRegex = new Regex("style=(\\\\|)\"([^\"]*)\"");

    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        var text = string.Empty;
        var nodeList = context.Value as XmlNodeList;

        if (nodeList != null)
        {
            text = nodeList.Item(0)?.InnerText;
        }
        else if (!string.IsNullOrEmpty(context.Value.ToString()) && context.Value.ToString().Length > 50)
        {
            text = context.Value.ToString();
        }

        if (string.IsNullOrEmpty(text))
            return null;

        var matches = baliseRegex.Matches(text);
        var styles = matches
            .OfType<Match>()
            .Select(x => x.Value)
            .Distinct();

        foreach (var style in styles)
        {
            text = text.Replace(style, string.Empty);
        }

        return text;
    }
}
