using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using Flow.Mapping.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Flow.Mapping;

public static class FlowHelper
{
    public static string SanitizeContent(string fileName, Encoding encoding = null)
    {
        const string goodAmpersand = "&amp;";

        encoding ??= IsUnicodeEncoded(File.ReadAllText(fileName)) ? Encoding.Unicode : Encoding.Default;

        var content = File.ReadAllText(fileName, encoding);

        if (!content.Contains(goodAmpersand))
        {
            var badAmpersandPattern = new Regex("&(?![a-zA-Z]{2,6};|#[0-9]{2,6};)");
            content = badAmpersandPattern.Replace(content, goodAmpersand);
        }

        return content;
    }

    public static string ConvertJsonToXml(string fileName, string baseTag = null, Encoding encoding = null)
    {
        var content = SanitizeContent(fileName, encoding);

        var root = JsonConvert.DeserializeObject<JObject>(content);

        baseTag ??= root.Properties().First().Name;

        root = new JObject
        {
            { baseTag, root }
        };

        var result = JsonConvert.DeserializeXmlNode(root.ToString()).OuterXml;
        return result;
    }

    private static bool IsUnicodeEncoded(string text)
    {
        // https://www.ascii-code.com/
        const int maxAnsiCode = 255; // ASCII character range (up to 127) Extended ASCII (ANSI) (128-255) 
        return text.Any(c => c > maxAnsiCode);
    }

    public static Dictionary<ValueResolverTypes, IFlowValueResolver> DefaultResolvers = new Dictionary<ValueResolverTypes, IFlowValueResolver>
    {
        { ValueResolverTypes.StringBool, new StringBoolResolver() },
        { ValueResolverTypes.StringDateFormat, new StringDateFormatResolver() },
        { ValueResolverTypes.DistinctByValue, new DistinctByValueResolver() },
        { ValueResolverTypes.MapIfDecimalValueGreaterThanZero, new MapIfDecimalValueGreaterThanZeroResolver() },

        { ValueResolverTypes.FrenchNumberFormat, new FrenchNumberResolver() },
        { ValueResolverTypes.FrenchCurrencyFormat, new FrenchCurrencyFormatResolver() },

        { ValueResolverTypes.ExtractFileNameFromUri, new ExtractFileNameFromUriResolver() },
        { ValueResolverTypes.HtmlStyleSanitizer, new HtmlStyleSanitizerResolver() },
        { ValueResolverTypes.UnescapeHtml, new UnescapeHtmlResolver() },

        { ValueResolverTypes.AverageValue, new AverageValueResolver() },
        { ValueResolverTypes.NumberToListOfMappedElement, new NumberToListOfMappedElementResolver() },
    };
}
