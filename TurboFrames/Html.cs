using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Html;

public readonly record struct Html : IHtmlContent
{
    string Value { get; }
    // public Html([LanguageInjection("HTML")] InterpolatedHtmlHandler Value)
    // {
    //     this.Value = Value.ToStringAndClear();
    // }

    public Html([LanguageInjection("HTML")]  string value) => this.Value = value;
    public static implicit operator string(Html html) => html.Value;
    public static implicit operator Html([LanguageInjection("HTML")] string html) => new(html);
    public override string ToString() => Value;
    
    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        writer.Write(Value);
    }
}

public record struct HtmlBuilder() : IEnumerable
{
    List<Html> _htmls = new();
    public void Add(Html html) => _htmls.Add(html);
    public void Add(IEnumerable<Html> htmls)
    {
        foreach(var html in htmls) 
            _htmls.Add(html);
    }
    public void Add([LanguageInjection("HTML")] string html) => _htmls.Add(new(html));

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var html in _htmls)
            sb.AppendLine(html.ToString());
        return sb.ToString();
    }

    public Html ToHtml() => new(ToString());

    public IEnumerator GetEnumerator() => throw new NotImplementedException();
}

[InterpolatedStringHandler]
public ref struct InterpolatedHtmlHandler(int literalLength, int formattedCount)
{
    DefaultInterpolatedStringHandler _default = new(literalLength, formattedCount);
    
    public void AppendLiteral(string value) => _default.AppendLiteral(value);

    public void AppendFormatted<T>(T value)
    {
        if (value is IEnumerable<Html> htmls)
        {
            foreach (var html in htmls)
            {
                _default.AppendLiteral(html);
                _default.AppendLiteral("\n");
            }

            return;
        }
        _default.AppendFormatted(value);
    }

    public string ToStringAndClear() => _default.ToStringAndClear();
}