using System.Reflection;

namespace SolidGroundClient;

public abstract class SolidGroundVariables
{
    internal SolidGroundVariable[] Variables
    {
        get
        {
            var fieldInfos = GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            return fieldInfos
                .Where(f => f.FieldType.IsAssignableTo(typeof(SolidGroundVariable)))
                .Select(f => f.GetValue(this))
                .Cast<SolidGroundVariable>()
                .ToArray();
        }
    }
}