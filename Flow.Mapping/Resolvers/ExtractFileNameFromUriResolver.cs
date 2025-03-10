using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;
using System.Web;
using System.Xml;


namespace Flow.Mapping.Resolvers;

public class ExtractFileNameFromUriResolver : IFlowValueResolver
{
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper)
    {
        var nodeList = context.Value as XmlNodeList;

        if (nodeList == null)
            return null;

        var text = nodeList.Item(0)?.InnerText;

        if (text == null)
            return null;

        if (text.Contains("%"))
            text = Uri.UnescapeDataString(text);

        if (!text.StartsWith("http") && !text.StartsWith("ftp"))
            text = $"file:///{text}";

        var splitUrl = text.Split("//");
        if (splitUrl.Length > 2)
            text = splitUrl.Skip(1).Aggregate($"{splitUrl[0]}/", (current, next) => current + "/" + next);

        Uri uri;
        if (!Uri.TryCreate(text, UriKind.Absolute, out uri))
        {
            try
            {
                uri = new Uri(text);
            }
            catch
            {
            }
        }

        var fileName = text;
        if (uri != null && !string.IsNullOrEmpty(uri.AbsolutePath))
        {
            if (!uri.IsFile)
                fileName = Path.GetFileName(uri.AbsolutePath);
            else
                fileName = Path.GetFileName(uri.OriginalString);
        }

        fileName = HttpUtility.UrlDecode(fileName);

        if (context.ValueResolverArguments != null && context.ValueResolverArguments.Any())
        {
            foreach (var resolver in context.ValueResolverArguments)
            {
                if (resolver.Name == "RemoveBetween")
                {
                    var removeBetween = resolver.Value?.Split("||");
                    fileName = RemoveBetween(fileName, removeBetween[0], removeBetween[1]);
                }

                if (resolver.Name == "Ext" && resolver.Value == "false")
                {
                    fileName = Path.GetFileNameWithoutExtension(fileName);
                }
            }
        }

        return fileName;
    }


    public string RemoveBetween(string input, string startStr, string endStr)
    {
        if (!input.Contains(startStr) || !input.Contains(endStr))
            return input;

        int start = input.LastIndexOf(startStr);
        int end = input.IndexOf(endStr, start);
        return input.Remove(start, end - start + endStr.Length);
    }
}
