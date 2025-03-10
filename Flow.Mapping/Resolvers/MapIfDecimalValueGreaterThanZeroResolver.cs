using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using System.Globalization;
using System.Xml;

namespace Flow.Mapping.Resolvers;

public class MapIfDecimalValueGreaterThanZeroResolver : IFlowValueResolver
{
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        bool isTypeList = context.DestinationType.IsGenericType && context.DestinationType.GetGenericTypeDefinition() == typeof(List<>);

        if (!isTypeList)
            return null; // not supported 

        var nodeList = context.Value as XmlNodeList;

        if (nodeList == null || nodeList.Count == 0)
            return null;

        var text = nodeList.Item(0)?.InnerText;
        if (text == null)
            return null;

        decimal result;
        bool parseResult;

        if (text.Contains("€"))
        {
            var nfi = new NumberFormatInfo();
            nfi.CurrencySymbol = "€";
            nfi.CurrencyDecimalSeparator = ",";
            nfi.CurrencyGroupSeparator = " ";

            parseResult = decimal.TryParse(text, NumberStyles.Any, nfi, out result);

            text = result.ToString();
        }
        else
        {
            text = text.Replace(",", ".");
            parseResult = decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }

        if (parseResult && result > 0)
        {
            var itemType = context.DestinationType.GetGenericArguments()[0];
            var newItem = Activator.CreateInstance(itemType);

            mapper.MapEntity(nodeList.Item(0), newItem);

            return newItem;
        }

        return null;
    }
}
