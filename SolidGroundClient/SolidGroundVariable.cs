using System.Globalization;

namespace SolidGroundClient;

public class SolidGroundVariable
{
    public string Name { get; }
    
    internal string ValueAsString
    {
        get
        {
            if (ActualValue is float f)
                return f.ToString(CultureInfo.InvariantCulture);
            
            return ActualValue.ToString() ?? throw new InvalidOperationException();
        }
    }

    protected object ActualValue;

    protected SolidGroundVariable(string Name, object DefaultValue)
    {
        ActualValue = DefaultValue;
        this.Name = Name;
    }
    
    internal void SetValue(string valueAsString)
    {
        object ValueFor()
        {
            var myGenericTypeArgument = MyGenericTypeArgument();
            
            if (myGenericTypeArgument == typeof(float))
                return float.Parse(valueAsString, CultureInfo.InvariantCulture);

            if (myGenericTypeArgument == typeof(string))
                return valueAsString;
            
            throw new NotSupportedException($"Variables of type {myGenericTypeArgument} are not supported.");
        }

        ActualValue = ValueFor();
    }

    Type MyGenericTypeArgument() => GetType().GetGenericArguments().Single();
}