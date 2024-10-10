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

    public static string HumanDate(this DateTime executionStartTime)
    {
        // Get the current date and time
        DateTime now = DateTime.Now;

        // If the execution start time is today
        if (executionStartTime.Date == now.Date)
        {
            return executionStartTime.ToString("h:mmtt").ToLower();
        }
        // If the execution start time was yesterday
        else if (executionStartTime.Date == now.Date.AddDays(-1))
        {
            return "Yesterday " + executionStartTime.ToString("h:mmtt").ToLower();
        }
        // If the execution start time was within the last 7 days
        else if ((now.Date - executionStartTime.Date).Days < 7)
        {
            return executionStartTime.ToString("dddd h:mmtt").ToLower();
        }
        // For dates older than 7 days
        else
        {
            return executionStartTime.ToString("MMMM d, yyyy h:mmtt").ToLower();
        }
    }

    public static string TailwindStyle(this Tag tag)
    {
        string[] options =
        [
            "bg-blue-100 text-blue-800 hover:bg-blue-200",
            "bg-green-100 text-green-800 hover:bg-green-200",
            "bg-yellow-100 text-yellow-800 hover:bg-yellow-200", 
            "bg-red-100 text-red-800 hover:bg-red-200",
            "bg-purple-100 text-purple-800 hover:bg-purple-200" 
        ];

        return options[tag.GetHashCode() % options.Length];
    }
    
    public static string ToTailwindColor(this ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Completed => "bg-green-50",
            ExecutionStatus.Started => "bg-blue-50",
            ExecutionStatus.Failed => "bg-red-50",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
    public static T GetRequired<T>(this JsonElement self, string propertyName)
    {
        if (!self.TryGetProperty(propertyName, out var jsonElement))
            throw new ArgumentException("Property not found", propertyName);

        return (T)Process(typeof(T), jsonElement);

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
}