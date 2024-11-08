using System.Text.RegularExpressions;
using JetBrains.Annotations;

record RouteTemplate
{
    readonly (string Placeholder, Type Type)[] _parameters;
    public string Value { get; }

    RouteTemplate(string value)
    {
        Value = value;
        _parameters = ValidateAndParseTemplate(value);
    }
    
    static readonly Regex Regex = new(@"(\{[^{}]+\})", RegexOptions.Compiled);
    
    static (string Placeholder, Type Type)[] ValidateAndParseTemplate(string template)
    {
        var matches = Regex.Matches(template);

        return matches.Select(match =>
        {
            var placeholder = match.Groups[1].Value;
            var parts = placeholder.Trim('{', '}').Split(':');
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid placeholder format: {placeholder}");

            var type = parts[1].ToLower() switch
            {
                "int" => typeof(int),
                "alpha" => typeof(string),
                "bool" => typeof(bool),
                _ => throw new ArgumentException($"Unsupported type: {parts[1]}")
            };
            return (placeholder, type);
        }).ToArray();
    }

    public static RouteTemplate Create([RouteTemplate] string value) => new(value);

    public override string ToString() => Value;

    public string For(params object?[] values)
    {
        if (values.Length != _parameters.Length)
            throw new ArgumentException($"Expected {_parameters.Length} parameters, but got {values.Length}");

        var result = Value;
        for (int i = 0; i < _parameters.Length; i++)
        {
            var (placeholder, type) = _parameters[i];
            var value = values[i];
            if (value == null)
                throw new ArgumentNullException();

            if (value.GetType() != type)
                throw new ArgumentException($"Expected type {type} for parameter {placeholder}, but got {value.GetType()}");

            result = result.Replace(placeholder, Uri.EscapeDataString(value.ToString()!));
        }

        return result;
    }

    public static implicit operator string(RouteTemplate routeTemplate) => routeTemplate.Value;
}
