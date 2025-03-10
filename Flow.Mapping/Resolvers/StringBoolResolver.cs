using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using System.Xml;

namespace Flow.Mapping.Resolvers;

public class StringBoolResolver : IFlowValueResolver
{
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        var nodeList = context.Value as XmlNodeList;
        if (nodeList == null)
            return false;

        var text = nodeList.Item(0)?.InnerText;
        if (text == null)
            return false;

        if (text.ToLower() == "o" || text.ToLower() == "oui" || text == "1" || text.ToLower() == "y" || text.ToLower() == "yes")
        {
            return true;
        }

        return false;
    }
}
