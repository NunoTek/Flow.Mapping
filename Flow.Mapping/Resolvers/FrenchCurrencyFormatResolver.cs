using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using System.Globalization;
using System.Xml;

namespace Flow.Mapping.Resolvers;

public class FrenchCurrencyFormatResolver : IFlowValueResolver
{
    // Gere les montants au format : 1 300 000,45 €
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        var nodeList = context.Value as XmlNodeList;

        if (nodeList == null)
            return null;

        var text = nodeList.Item(0)?.InnerText;

        if (text == null)
            return null;

        var nfi = new NumberFormatInfo();
        nfi.CurrencySymbol = "€";
        nfi.CurrencyDecimalSeparator = ",";
        nfi.CurrencyGroupSeparator = " ";

        decimal.TryParse(text, NumberStyles.Any, nfi, out decimal result);

        return result;
    }
}
