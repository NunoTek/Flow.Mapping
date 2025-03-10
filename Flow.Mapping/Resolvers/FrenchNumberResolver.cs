using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using System.Globalization;
using System.Xml;

namespace Flow.Mapping.Resolvers;

public class FrenchNumberResolver : IFlowValueResolver
{
    // Gere les decimals/double au format : 900,45
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        var nodeList = context.Value as XmlNodeList;
        if (nodeList == null)
            return null;

        var text = nodeList.Item(0)?.InnerText;
        if (text == null)
            return null;

        var nfi = new NumberFormatInfo();
        nfi.CurrencyDecimalSeparator = ",";

        var type = context.DestinationType;
        // Gere les props nullable
        if (Nullable.GetUnderlyingType(type) != null)
            type = Nullable.GetUnderlyingType(type);

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Double:
                double.TryParse(text, NumberStyles.Any, nfi, out double doubleResult);
                return doubleResult;

            case TypeCode.Decimal:
                decimal.TryParse(text, NumberStyles.Any, nfi, out decimal decimalResult);
                return decimalResult;

            default:
                return null;
        }
    }
}
