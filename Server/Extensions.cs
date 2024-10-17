using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace SolidGround;

public static class Extensions
{
    public static async Task<byte[]> ToBytesAsync(this Stream stream)
    {
        using var temp = new MemoryStream();
        await stream.CopyToAsync(temp);
        return temp.ToArray();
    }

    public static string GetMandatory(this IConfiguration config, string keyname) => config[keyname] ?? throw new Exception($"No config found for {keyname}");
    
    public static bool TryGetOptional<T>(this JsonElement self, string propertyName, out T? value)
    {
        if (!self.TryGetProperty(propertyName, out var jsonElement))
        {
            value = default;
            return false;
        }

        value = (T)Process(typeof(T), jsonElement);
        return true;

        object Process(Type type, JsonElement element)
        {
            if (type.IsArray)
            {
                if (element.ValueKind != JsonValueKind.Array)
                    throw new ArgumentException("Property is not an array", propertyName);
                
                return element.EnumerateArray().Select(o => Process(type.GetElementType()!, o)).ToArray();
            }

            if (type == typeof(string))
            {
                if (element.ValueKind != JsonValueKind.String)
                    throw new ArgumentException("Property is not a string", propertyName);
                return element.GetString()!;
            }

            if (type == typeof(JsonElement))
                return element;
            
            throw new NotSupportedException($"Property of type {typeof(T).Name} is not yet supported");
        }
    }

    public static string SeparateWith(this IEnumerable<string> values, string seperator)
    {
        return string.Join(seperator, values);
    }
    
    public static T GetRequired<T>(this JsonElement self, string propertyName)
    {
        if (!self.TryGetOptional<T>(propertyName, out var value))
            throw new ArgumentException("Property not found", propertyName);
        if (value == null)
            throw new ArgumentException($"Property {propertyName} was found but was null");
        return value;    
    }
}