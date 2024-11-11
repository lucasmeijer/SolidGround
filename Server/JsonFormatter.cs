using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Html;

public static class JsonFormatter
{
    public static Html FormatMaybeJson(string? maybeJson)
    {
        if (maybeJson == null)
            return new ("<pre class=\"json-highlight whitespace-pre-wrap break-all\">Null</pre>");
        try
        {
            var doc = JsonDocument.Parse(maybeJson);
            return FormatJsonToHtml(doc);
        }
        catch (JsonException)
        {
            return new ($"<pre class=\"json-highlight whitespace-pre-wrap break-all\"><code>{EscapeTags(maybeJson)}</code></pre>");
        }
    }

    static string EscapeTags(string content)
    {
        return content
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
    
    static Html FormatJsonToHtml(JsonDocument jsonDocument)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("<pre class=\"json-highlight whitespace-pre-wrap break-all\">");
        FormatJsonElement(jsonDocument.RootElement, stringBuilder, 0);
        stringBuilder.Append("</pre>");
        return new(stringBuilder.ToString());
    }

    static void FormatJsonElement(JsonElement element, StringBuilder sb, int indent)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                FormatObject(element, sb, indent);
                break;
            case JsonValueKind.Array:
                FormatArray(element, sb, indent);
                break;
            case JsonValueKind.String:
                sb.Append("<span class=\"json-string\">\"").Append(HttpUtility.HtmlEncode(element.GetString())).Append("\"</span>");
                break;
            case JsonValueKind.Number:
                sb.Append("<span class=\"json-number\">").Append(element.GetRawText()).Append("</span>");
                break;
            case JsonValueKind.True:
                sb.Append("<span class=\"json-boolean\">true</span>");
                break;
            case JsonValueKind.False:
                sb.Append("<span class=\"json-boolean\">false</span>");
                break;
            case JsonValueKind.Null:
                sb.Append("<span class=\"json-null\">null</span>");
                break;
            case JsonValueKind.Undefined:
                sb.Append("<span class=\"json-undefined\">Undefined</span>");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    static void FormatObject(JsonElement element, StringBuilder sb, int indent)
    {
        
        sb.Append("""<span class="json-bracket">{</span>""");
        bool isFirst = true;
        foreach (var property in element.EnumerateObject())
        {
            if (!isFirst)
            {
                sb.Append(",");
            }
            sb.AppendLine();
            sb.Append(new string(' ', (indent + 1) * 2));
            sb.Append("<span class=\"json-property\">\"").Append(HttpUtility.HtmlEncode(property.Name)).Append("\"</span>: ");
            FormatJsonElement(property.Value, sb, indent + 1);
            isFirst = false;
        }
        if (!isFirst)
        {
            sb.AppendLine();
            sb.Append(new string(' ', indent * 2));
        }
        sb.Append("""<span class="json-bracket">}</span>""");
    }

    private static void FormatArray(JsonElement element, StringBuilder sb, int indent)
    {
        sb.Append("[");
        bool isFirst = true;
        foreach (var item in element.EnumerateArray())
        {
            if (!isFirst)
            {
                sb.Append(",");
            }
            sb.AppendLine();
            sb.Append(new string(' ', (indent + 1) * 2));
            FormatJsonElement(item, sb, indent + 1);
            isFirst = false;
        }
        if (!isFirst)
        {
            sb.AppendLine();
            sb.Append(new string(' ', indent * 2));
        }
        sb.Append("]");
    }
}
