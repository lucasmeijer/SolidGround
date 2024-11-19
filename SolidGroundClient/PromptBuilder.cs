using System.Collections;
using System.Text.RegularExpressions;

namespace SolidGroundClient;

public class PromptBuilder : IEnumerable
{
    readonly string _template;
    readonly bool _allowUnusedVariables;
    readonly HashSet<string> _requiredVariables;
    readonly Dictionary<string, string> _providedValues;

    public PromptBuilder(string template, bool allowUnusedVariables = false)
    {
        _template = template;
        _allowUnusedVariables = allowUnusedVariables;
        _providedValues = new();
        _requiredVariables = ExtractVariables(template);
    }

    HashSet<string> ExtractVariables(string template)
    {
        var matches = Regex.Matches(template, @"\*\*(\w+)\*\*");
        var variables = new HashSet<string>();
            
        foreach (Match match in matches)
        {
            variables.Add(match.Groups[1].Value);
        }
            
        return variables;
    }

    public PromptBuilder Add(string variable, string value)
    {
        if (!_requiredVariables.Contains(variable))
        {
            if (!_allowUnusedVariables)
                throw new ArgumentException($"Variable **{variable}** was not found in the original template");
            return this;
        }

        _providedValues[variable] = value;
        return this;
    }

    public string Build()
    {
        var missingVariables = new HashSet<string>(_requiredVariables);
        missingVariables.ExceptWith(_providedValues.Keys);

        if (missingVariables.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing values for variables: {string.Join(", ", missingVariables)}");
        }

        string result = _template;
        foreach (var kvp in _providedValues)
        {
            result = result.Replace($"**{kvp.Key}**", kvp.Value);
        }

        return result;
    }

    //only to get collection initializer syntax
    public IEnumerator GetEnumerator() => throw new NotImplementedException();
}