using System.Reflection;
using SolidGround;

namespace SolidGroundClient;

public abstract class SolidGroundVariables
{
    public virtual string? ApplicationInformation => null;
    public virtual string? PromptingGuidelines => null;
    public virtual EvaluationCriterion[] EvaluationCriteria => [];
    
    public IEnumerable<PropertyInfo> Properties => GetType()
        .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
        .Where(p=>!(p.GetMethod?.IsVirtual ?? false))
        .Where(p=>p.DeclaringType != typeof(SolidGroundVariables));
    
    public void SetPropertyAsString(PropertyInfo p, string valueAsString)
    {
        if (p.PropertyType == typeof(bool))
        {
            bool b;
            if (string.Equals(valueAsString, "true", StringComparison.OrdinalIgnoreCase))
                b = true;
            else if (string.Equals(valueAsString, "false", StringComparison.OrdinalIgnoreCase))
                b = false;
            else 
                throw new ArgumentException($"Property {p.Name} is bool but is being assigned '{valueAsString}'");
            p.SetValue(this, b);
            return;
        }

        if (p.PropertyType.IsEnum)
        {
            var value = Enum.Parse(p.PropertyType, valueAsString, true);
            p.SetValue(this, value);
            return;
        }
        p.SetValue(this, valueAsString);
    }

    public string GetPropertyAsString(PropertyInfo propertyInfo)
    {
        var value = propertyInfo.GetValue(this) ?? throw new InvalidOperationException();
        
        if (propertyInfo.PropertyType == typeof(bool))
            return (bool)value ? "true" : "false";

        if (propertyInfo.PropertyType.IsEnum)
            return Enum.GetName(propertyInfo.PropertyType, value) ?? throw new InvalidOperationException();
        
        return (string)value;
    }

    public string[]? GetPropertyOptions(PropertyInfo prop)
    {
        if (prop.PropertyType.IsEnum)
            return Enum.GetNames(prop.PropertyType);
        if (prop.PropertyType == typeof(bool))
            return ["true", "false"];
        return null;
    }
}
