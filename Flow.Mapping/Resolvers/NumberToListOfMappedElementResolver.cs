using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using System.Text.RegularExpressions;
using System.Xml;

namespace Flow.Mapping.Resolvers;

public class NumberToListOfMappedElementResolver : IFlowValueResolver
{
    // Retourne une list avec autant d'élement que la valeur de la node
    // Si la valeur de la node est de 3 alors on retourne une List qui contiendras 3 items
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        if (context.Value == null)
            return null;

        var nodeList = context.Value as XmlNodeList;
        if (nodeList == null || nodeList.Count == 0)
            return null;

        var childNode = nodeList.Item(0);
        var innerText = childNode.InnerText;

        var itemType = context.DestinationType.GetGenericArguments()[0];
        var result = Activator.CreateInstance(context.DestinationType);

        if (string.IsNullOrEmpty(innerText))
        {
            return result;
        }


        void MapEntity()
        {
            var newItem = Activator.CreateInstance(itemType);
            context.DestinationType.GetMethod("Add").Invoke(result, new[] { newItem });

            mapper.MapEntity(childNode, newItem);
        }

        var countMatch = Regex.Match(innerText, @"\d+");
        if (!countMatch.Success)
        {
            MapEntity();
        }

        int count = int.Parse(countMatch.Value); // The number of times to repeat

        for (int i = 0; i < count; i++)
        {
            MapEntity();
        }

        return result;
    }
}
