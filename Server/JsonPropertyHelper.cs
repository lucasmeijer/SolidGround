using System.Linq.Expressions;
using System.Reflection;

namespace SolidGround;

public static class JsonPropertyHelper
{
    public static string JsonNameFor(LambdaExpression propertyExpression)
    {
        // var options = serviceProvider.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
        
        if (propertyExpression.Body is not MemberExpression memberExpression)
            throw new ArgumentException("Expression must be a member access", nameof(propertyExpression));

        // var declaringType = memberExpression.Member.DeclaringType!;
        var propertyInfo = memberExpression.Member as PropertyInfo;
        
        if (propertyInfo == null)
            throw new ArgumentException("Expression must refer to a property", nameof(propertyExpression));

        var attr = propertyInfo
                       .GetCustomAttributes<System.Text.Json.Serialization.JsonPropertyNameAttribute>()
                       .FirstOrDefault()
                   ?? throw new ArgumentException("It's mandatory to put an attribute on the property until we come up with something smarter");
        return attr.Name;
    }
}