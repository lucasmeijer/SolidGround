using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Html;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using TurboFrames;

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

    public static void Add(this HttpContentHeaders self, IEnumerable<KeyValuePair<string, string>> values)
    {
        foreach(var kvp in values)
            self.Add(kvp.Key, kvp.Value);
    }
    
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
    
    public static Task<string> RenderAsync(this IEnumerable<TurboFrame> turboFrames, IServiceProvider serviceProvider)
    {
        return turboFrames.RenderAsync(tf => tf.RenderAsync(serviceProvider));
    }

    public static string Render(this IEnumerable<Html> source) => source.Select(s => s.ToString()).SeparateWith("\n");
    
    public static string Render<TSource>(this IEnumerable<TSource> source, Func<TSource, Html> xform) => source.Select(xform).Render();
    
    public static async Task<string> RenderAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, Task<Html>> xform)
    {
        var htmls = await Task.WhenAll(source.Select(xform));
        return htmls.Render();
    }


    public static T GetRequired<T>(this JsonElement self, string propertyName)
    {
        if (!self.TryGetOptional<T>(propertyName, out var value))
            throw new BadHttpRequestException($"Property {propertyName} not found");
        if (value == null)
            throw new BadHttpRequestException($"Property {propertyName} was found but was null");
        return value;    
    }
    
    public static string Color(this Tag tag)
    {
        //bool hasTag = tag.Id % 2 == 1;
        return (tag.Id % 3) switch
        {
            0 => "red",
            1 => "green",
            2 => "blue",
            _ => "gray" // Default case
        };
    }
}
