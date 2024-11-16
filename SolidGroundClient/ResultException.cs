using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SolidGroundClient;

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
            catch (Exception ex)
            {
                var session = context.RequestServices.GetService<SolidGroundSession>();
                if (session is { IsSolidGroundInitiated: true })
                {
                    var problem = Results.Problem(ex.ToString(), null, StatusCodes.Status500InternalServerError,
                        "Exception");
                    await problem.ExecuteAsync(context);
                }
                else
                    throw;
            }
        });
    }
}