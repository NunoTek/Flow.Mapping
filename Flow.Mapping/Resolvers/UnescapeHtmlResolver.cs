using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using System.Web;
using System.Xml;

namespace Flow.Mapping.Resolvers;

public class UnescapeHtmlResolver : IFlowValueResolver
{
    // Traite    &amp;#x26;lt;br&amp;#x26;gt;&amp;#x26;lt;br&amp;#x26;gt;
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        var nodeList = context.Value as XmlNodeList;

        var text = nodeList.Item(0)?.InnerText;

        if (text == null)
            return null;

        var result = text;

        var limit = 5;
        for (int i = 0; i <= limit; i++)
        {
            if (result.Contains("<") || result.Contains("é"))
                break;

            result = HttpUtility.HtmlDecode(result);
        }

        return result;
    }
}
