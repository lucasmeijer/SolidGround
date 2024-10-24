using System.Linq.Expressions;
using System.Reflection;

public static class ServiceProviderHelper
{
    public static Func<IServiceProvider, Task<TReturnType>> CompileInjectionFor<TReturnType>(Delegate d)
    {
        var method = d.Method;
        var parameters = method.GetParameters();
        var serviceProviderExpression = Expression.Parameter(typeof(IServiceProvider), "provider");

        Expression[] argumentExpressions = [..parameters.Select(p =>
            Expression.Convert(
                Expression.Call(serviceProviderExpression,
                    typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!,
                    Expression.Constant(p.ParameterType)),
                p.ParameterType))];

        Expression callExpression = method.IsStatic 
            ? Expression.Call(method, argumentExpressions) 
            : Expression.Call(Expression.Constant(d.Target), method, argumentExpressions);
        
        return Expression.Lambda<Func<IServiceProvider, Task<TReturnType>>>(ResultExpression<TReturnType>(method, callExpression), serviceProviderExpression).Compile();
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