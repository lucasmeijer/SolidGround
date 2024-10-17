// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.Abstractions;
// using Microsoft.AspNetCore.Mvc.ModelBinding;
// using Microsoft.AspNetCore.Mvc.Razor;
// using Microsoft.AspNetCore.Mvc.Rendering;
// using Microsoft.AspNetCore.Mvc.ViewFeatures;
//
//
// public class ViewRenderService
// {
//     private readonly IRazorViewEngine _viewEngine;
//     private readonly ITempDataProvider _tempDataProvider;
//
//     public ViewRenderService(
//         IRazorViewEngine viewEngine,
//         ITempDataProvider tempDataProvider)
//     {
//         _viewEngine = viewEngine;
//         _tempDataProvider = tempDataProvider;
//     }
//
//     public async Task<string> RenderPartialViewToStringAsync(HttpContext httpContext, string viewName, object model)
//     {
//         var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());
//
//         using var sw = new StringWriter();
//
//         var viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: false);
//
//         if (viewResult.View == null)
//         {
//             throw new InvalidOperationException($"View '{viewName}' not found.");
//         }
//
//         var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
//         {
//             Model = model
//         };
//
//         var tempData = new TempDataDictionary(httpContext, _tempDataProvider);
//
//         var viewContext = new ViewContext(
//             actionContext,
//             viewResult.View,
//             viewDictionary,
//             tempData,
//             sw,
//             new HtmlHelperOptions()
//         );
//
//         await viewResult.View.RenderAsync(viewContext);
//
//         return sw.ToString();
//     }
// }
//
// public record TurboStreamViewAndModel(string View, object Model);
// public class TurboStreamResult(params TurboStreamViewAndModel[] elements) : IResult
// {
//     public async Task ExecuteAsync(HttpContext httpContext)
//     {
//         var viewRenderService = httpContext.RequestServices.GetRequiredService<ViewRenderService>();
//         httpContext.Response.ContentType = "text/vnd.turbo-stream.html";
//         foreach (var element in elements)
//         {
//             var html = await viewRenderService.RenderPartialViewToStringAsync(httpContext, element.View, element.Model);
//             await httpContext.Response.WriteAsync(html);
//         }
//     }
// }
//
// public class ViewResult(string viewName, object model) : IResult
// {
//     public async Task ExecuteAsync(HttpContext httpContext)
//     {
//         var razorViewEngine = httpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
//         var tempDataProvider = httpContext.RequestServices.GetRequiredService<ITempDataProvider>();
//         
//         var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());
//
//         using var sw = new StringWriter();
//
//         var viewResult = razorViewEngine.FindView(actionContext, viewName, isMainPage: false);
//
//         if (viewResult.View == null)
//         {
//             throw new InvalidOperationException($"View '{viewName}' not found.");
//         }
//
//         var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
//         {
//             Model = model
//         };
//
//         var tempData = new TempDataDictionary(httpContext, tempDataProvider);
//
//         var viewContext = new ViewContext(
//             actionContext,
//             viewResult.View,
//             viewDictionary,
//             tempData,
//             sw,
//             new HtmlHelperOptions()
//         );
//
//         await viewResult.View.RenderAsync(viewContext);
//
//         return sw.ToString();
//         
//         var html = await viewRenderService.RenderPartialViewToStringAsync(httpContext, viewName, model);
//         httpContext.Response.ContentType = "text/html";
//         await httpContext.Response.WriteAsync(html);
//     }
// }