using System.Linq.Expressions;
using System.Reflection;

public static class ServiceProviderHelper
{
    public static Func<IServiceProvider, Task<TReturnType>> CompileInjectionFor<TReturnType>(Delegate d)
    {
        var method = d.Method;
        var parameters = method.GetParameters();
        var serviceProviderExpression = Expression.Parameter(typeof(IServiceProvider), "provider");

        Expression[] argumentExpressions = new Expression[parameters.Length];
    
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var serviceExp = Expression.Call(
                serviceProviderExpression,
                typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!,
                Expression.Constant(parameter.ParameterType));

            var convertedExp = Expression.Convert(serviceExp, parameter.ParameterType);
        
            var throwExp = Expression.Throw(
                Expression.New(
                    typeof(InvalidOperationException).GetConstructor([typeof(string)])!,
                    Expression.Constant($"Cannot inject parameter '{parameter.Name}' of type '{parameter.ParameterType}'. The service is not registered.")),
                parameter.ParameterType);

            argumentExpressions[i] = Expression.Condition(
                Expression.Equal(serviceExp, Expression.Constant(null)),
                throwExp,
                convertedExp);
        }

        Expression callExpression = method.IsStatic 
            ? Expression.Call(method, argumentExpressions) 
            : Expression.Call(Expression.Constant(d.Target), method, argumentExpressions);

        return Expression.Lambda<Func<IServiceProvider, Task<TReturnType>>>(
            ResultExpression<TReturnType>(method, callExpression), 
            serviceProviderExpression).Compile();
    }


    static Expression ResultExpression<TReturnType>(MethodInfo method, Expression callExpression)
    {
        switch (method.ReturnType)
        {
            case var t when t == typeof(void):
                throw new NotSupportedException("void returning methods are not supported");
            case var t when t == typeof(Task):
                throw new NotSupportedException("nongeneric Task returning methods are not supported");
            case var t when t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>):
                
                var genericArgument = t.GetGenericArguments()[0];

                VerifyReturnType<TReturnType>(genericArgument);
                
                return Expression.Call(
                    typeof(ServiceProviderHelper).GetMethod(nameof(WrapTaskResult),
                        BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(genericArgument),
                    callExpression);
            
            default:
                VerifyReturnType<TReturnType>(method.ReturnType);
                return Expression.Call(
                    typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(typeof(TReturnType)),
                    Expression.Convert(callExpression, typeof(TReturnType)));
        }
    }

    static void VerifyReturnType<TReturnType>(Type actualReturnType)
    {
        if (!actualReturnType.IsAssignableTo(typeof(TReturnType)))
            throw new ArgumentException($"Expected a delegate returning {typeof(TReturnType).Name}, but got {actualReturnType.Name}");
    }

    static async Task<T> WrapTaskResult<T>(Task<T> task) => await task;
}