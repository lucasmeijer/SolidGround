using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SolidGround;

class ResultException(IResult result) : Exception
{
    public IResult Result { get; } = result;
}

static class ResultExceptionExtensions
{
    public static void UseResultException(this IApplicationBuilder webApplication)
    {
        webApplication.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (ResultException ex)
            {
                await ex.Result.ExecuteAsync(context);
            }
        });
    }
}