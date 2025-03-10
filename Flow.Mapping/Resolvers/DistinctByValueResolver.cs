using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using System.Xml;

namespace Flow.Mapping.Resolvers;

public class DistinctByValueResolver : IFlowValueResolver
{
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        if (context.Value == null)
            return null;

        var nodes = context.Value as XmlNodeList;
        if (nodes == null || nodes.Count == 0)
            return null;

        var nodeList = new List<XmlNode>(nodes.Cast<XmlNode>()).DistinctBy(x => x.InnerText).ToList();


        var itemType = context.DestinationType.GetGenericArguments()[0];
        var result = Activator.CreateInstance(context.DestinationType);

        foreach (XmlNode node in nodeList)
        {
            if (string.IsNullOrEmpty(node.InnerText))
                continue;

            var newItem = Activator.CreateInstance(itemType);
            mapper.MapEntity(node, newItem);

            context.DestinationType.GetMethod("Add").Invoke(result, new[] { newItem });
        }

        return result;
    }
}
