using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SolidGround.Pages;
using TurboFrames;

namespace SolidGround;

public static class LoginEndPoints
{
    public static void MapLoginEndPoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/login", () =>
        {
            return new SolidGroundPage("Login", new LoginBodyContent());
        }).AllowAnonymous();
        app.MapPost("/login", async (HttpContext context, [FromForm] string username, [FromForm] string password) =>
            {
                if (password != "1234")
                {
                    return Results.Unauthorized();
                }

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.Role, "User")
                    ], CookieAuthenticationDefaults.AuthenticationScheme)));
                
                return Results.Ok();
            })
            .DisableAntiforgery()
            .AllowAnonymous();
    }
}

record LoginBodyContent : PageFragment
{
    public override Task<Html> RenderAsync(IServiceProvider serviceProvider)
    {
        return Task.FromResult(new Html("""
                                        <div class="flex items-center justify-center h-screen">
                                        <div class="max-w-md w-full space-y-8 p-8 bg-white rounded-lg shadow-lg">
                                            <div class="text-center">
                                                <h2 class="mt-6 text-3xl font-extrabold text-gray-900">
                                                    Sign in to your account
                                                </h2>
                                            </div>
                                            
                                            <turbo-frame id="login-form">
                                                <form class="mt-8 space-y-6" action="/login" method="POST" data-turbo="false">
                                                    <div class="rounded-md shadow-sm space-y-4">
                                                        <div>
                                                            <label for="username" class="sr-only">Username</label>
                                                            <input id="username" name="username" type="text" required 
                                                                class="appearance-none rounded-lg relative block w-full px-3 py-2 border 
                                                                border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none 
                                                                focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm" 
                                                                placeholder="Username">
                                                        </div>
                                                        <div>
                                                            <label for="password" class="sr-only">Password</label>
                                                            <input id="password" name="password" type="password" required 
                                                                class="appearance-none rounded-lg relative block w-full px-3 py-2 border 
                                                                border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none 
                                                                focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm" 
                                                                placeholder="Password">
                                                        </div>
                                                    </div>
                                        
                                                    <div>
                                                        <button type="submit" 
                                                            class="group relative w-full flex justify-center py-2 px-4 border border-transparent 
                                                            text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 
                                                            focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                                                            Sign in
                                                        </button>
                                                    </div>
                                                </form>
                                            </turbo-frame>
                                        </div>
                                        </div>
                                        """));
    }
}