using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Flow.Mapping.Resolvers;

public class StringDateFormatResolver : IFlowValueResolver
{
    // Parse date in TQ yyyy format
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        var value = context.Value as string;
        var nodeList = context.Value as XmlNodeList;
        if (nodeList == null && string.IsNullOrEmpty(value))
            return null;

        string text = value ?? nodeList.Item(0)?.InnerText;
        if (string.IsNullOrEmpty(text))
            return null;


        if (context.ValueResolverArguments == null || context.ValueResolverArguments.Count == 0)
        {
            context.ValueResolverArguments = new List<ResolverArgument>() { new ResolverArgument() { Name = "Default", Value = "d" } };
        }
        else
        {
            context.ValueResolverArguments.Add(new ResolverArgument() { Name = "Default", Value = "d" });
        }

        DateTimeOffset? result = null;

        foreach (var format in context.ValueResolverArguments)
        {
            string formatvalue = format.Value;

            try
            {
                if (formatvalue.ToLower() == "xml")
                {
                    result = XmlConvert.ToDateTimeOffset(text);
                    break;
                }
                //Gestion du format en entré de type trimestre/année (42018)
                if (formatvalue.ToLower().Count(x => x == 'q') == 1 && formatvalue.ToLower().Contains("yy"))
                {
                    if (formatvalue.Trim().Length > text.Trim().Length)
                    {
                        continue;
                    }

                    int.TryParse(text.Substring(formatvalue.IndexOf("q"), 1), out int quarterNumber);

                    //si format de type 2ème semestre 2021
                    if (formatvalue.ToLower().Contains("semestre"))
                    {
                        quarterNumber *= 2;
                    }

                    int yearsNumber = 0;
                    if (formatvalue.ToLower().Contains("yyyy"))
                    {
                        int.TryParse(text.Substring(formatvalue.IndexOf("yyyy"), 4), out yearsNumber);
                    }
                    else
                    {
                        int.TryParse(text.Substring(formatvalue.IndexOf("yy"), 2), out yearsNumber);
                        yearsNumber += 2000;
                    }

                    int monthNumber = quarterNumber * 3;
                    int lastDayOfMonth = DateTime.DaysInMonth(yearsNumber, monthNumber);

                    var dateTime = new DateTime(yearsNumber, monthNumber, lastDayOfMonth);
                    result = new DateTimeOffset(dateTime);
                    break;
                }
                //Gestion du format en entré de type trimestre/année (T4 2018) / (4T 2018)
                else if (formatvalue.ToLower().Count(x => x == 'q') == 2 && formatvalue.ToLower().Contains("yy"))
                {
                    if (formatvalue.Trim().Length > text.Trim().Length)
                    {
                        continue;
                    }

                    int quarterNumber = 0;
                    if (text.Substring(formatvalue.IndexOf("q"), 1) == "T")
                    {
                        int.TryParse(text.Substring(formatvalue.IndexOf("q") + 1, 1), out quarterNumber);
                    }

                    int yearsNumber = DateTimeOffset.Now.Year;
                    if (formatvalue.ToLower().Contains("yyyy"))
                    {
                        int.TryParse(text.Substring(formatvalue.IndexOf("yyyy"), 4), out yearsNumber);
                    }
                    else if (formatvalue.ToLower().Contains("yy"))
                    {
                        int.TryParse(text.Substring(formatvalue.IndexOf("yy"), 2), out yearsNumber);
                        yearsNumber += 2000;
                    }

                    int monthNumber = quarterNumber * 3;
                    int lastDayOfMonth = DateTime.DaysInMonth(yearsNumber, monthNumber);

                    var dateTime = new DateTime(yearsNumber, monthNumber, lastDayOfMonth);
                    result = new DateTimeOffset(dateTime);
                    break;
                }
                //Gestion du format en entrée de type moi/année (Septembre2018)
                else if (formatvalue.ToLower().Count(x => x == 'm') == 4 && formatvalue.ToLower().Contains("yy"))
                {
                    var culture = new CultureInfo("fr-FR");
                    var dateTime = DateTimeOffset.ParseExact(text, formatvalue, culture);

                    int lastDayOfMonth = DateTime.DaysInMonth(dateTime.Year, dateTime.Month);

                    result = new DateTime(dateTime.Year, dateTime.Month, lastDayOfMonth);
                    break;
                }
                //Gestion des formats standard de date
                else
                {
                    var culture = new CultureInfo("fr-FR");
                    if (new Regex(@"\d{2}\/\d{2}\/\d{4} \d{2}:\d{2}").IsMatch(text))
                    {
                        text = text.Split(" ")[0];
                    }

                    result = DateTimeOffset.ParseExact(text, formatvalue, culture);
                    break;
                }
            }
            catch
            {

            }
        }

        return result;
    }
}
