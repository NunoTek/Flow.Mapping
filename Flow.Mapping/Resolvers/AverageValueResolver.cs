using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using System.Globalization;
using System.Xml;


namespace Flow.Mapping.Resolvers;

public class AverageValueResolver : IFlowValueResolver
{
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        var nodeList = context.Value as XmlNodeList;

        if (nodeList == null || nodeList.Count == 0)
        {
            if (Nullable.GetUnderlyingType(context.DestinationType) != null)
                return null;
            return 0;
        }

        var typeUnboxed = Nullable.GetUnderlyingType(context.DestinationType) ?? context.DestinationType;

        if (typeUnboxed == typeof(decimal))
        {
            return nodeList.Cast<XmlNode>().Select(n => { decimal.TryParse(n.InnerText, out decimal value); return value; }).Sum() / nodeList.Count;
        }
        else if (typeUnboxed == typeof(double))
        {
            return nodeList.Cast<XmlNode>().Select(n => { double.TryParse(n.InnerText, NumberStyles.Any, CultureInfo.InvariantCulture, out double value); return value; }).Sum() / nodeList.Count;
        }
        else if (typeUnboxed == typeof(int))
        {
            return nodeList.Cast<XmlNode>().Select(n => { int.TryParse(n.InnerText, out int value); return value; }).Sum() / nodeList.Count;
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}
