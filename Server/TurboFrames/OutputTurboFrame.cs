using Microsoft.EntityFrameworkCore;
using SolidGround.Pages;
using TurboFrames;

namespace SolidGround;

public record OutputTurboFrame(int OutputId) : TurboFrame(TurboFrameIdFor(OutputId))
{
    public static string TurboFrameIdFor(int outputId) => $"output_{outputId}";
    
    protected override async Task<Html> RenderContentsAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var output = await dbContext.Outputs
                         .Include(o => o.Components)
                         .Include(o => o.Execution)
                         .Include(o => o.StringVariables)
                         .AsSplitQuery()
                         .FirstOrDefaultAsync(o => o.Id == OutputId)
                     ?? throw new BadHttpRequestException("No output found");
        return new($"""
            <div class="flex flex-row gap-2 items-stretch">
                <details class="bg-gray-50 flex-1 shadow-md rounded-lg group/output {ColorFor(output)}">
                    <summary class="p-4 cursor-pointer flex justify-between items-center rounded-lg ">
                        <h3 class="font-semibold">{HowMuchTimeAgo(output.Execution.StartTime)}</h3>
                        <svg class="w-5 h-5 transition-transform duration-200 group-open/output:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                        </svg>
                    </summary>
                    <div class="p-2">
                        <div class="flex gap-2">
                            {ResultHtmlsFor(output).Render()}
                            <a href="{RunExperimentTurboFrame.RouteFor(output.Id)}" data-turbo-frame="{RunExperimentTurboFrame.TurboFrameId}" class="text-sm bg-green-200 h-16 p-2 rounded-lg">
                                Adopt variables for new experiment
                            </a>
                        </div>
                        <details class="my-4">    
                            <summary class="text-sm">Details</summary>
                            <div class="p-4">
                                {output.Components.Where(c => c.Name != "result").Render(RenderComponent)}
                                <br/>
                                <br/>
                                <br/>
                                {output.StringVariables.Render(RenderStringVariable)}
                            </div>
                        </details>
                    </div>
                </details>
                <a href="/api/executions/{output.ExecutionId}" data-turbo-method="delete" class="{Buttons.Attrs} {Buttons.RedAttrs}">
                    Delete
                </a>
            </div>
            """);
    }

    static Html[] ResultHtmlsFor(Output output)
    {
        var result = output.Components.FirstOrDefault(c => c.Name == "result");
        return result == null 
            ? [] 
            : [new($"""
                    <div class="text-xs">
                        {JsonFormatter.FormatMaybeJson(result.Value)}
                    </div>
                    """)];
    }

    static Html RenderStringVariable(StringVariable stringVariable) => new($"""
        <details class="p-2 border-b last:border-b-0 text-sm group/component">
            <summary class="cursor-pointer flex justify-between items-center hover:bg-gray-50">
                <h3 class="font-semibold">{stringVariable.Name}</h3>
                <svg class="w-5 h-5 transition-transform duration-200 transform rotate-0 group-open/component:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                </svg>
            </summary>
            <div class="py-2 text-xs">
                {JsonFormatter.FormatMaybeJson(stringVariable.Value)}
            </div>
        </details>
        """);

    static Html RenderComponent(OutputComponent c) => new($"""
               <details class="p-2 border-b last:border-b-0 text-sm group/component">
                   <summary class="cursor-pointer flex justify-between items-center hover:bg-gray-50">
                       <h3 class="font-semibold">{c.Name}</h3>
                       <svg class="w-5 h-5 transition-transform duration-200 transform rotate-0 group-open/component:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                           <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                       </svg>
                   </summary>
                   <div class="py-2 text-xs">
                       {JsonFormatter.FormatMaybeJson(c.Value)}
                   </div>
               </details>
           """);

    static string ColorFor(Output output) => output.Status switch
    {
        ExecutionStatus.Started => "bg-blue-200",
        ExecutionStatus.Failed => "bg-red-200",
        _ => ""
    };

    static string HowMuchTimeAgo(DateTime dateTime)
    {
        var now = DateTime.Now;
        var difference = now - dateTime;

        if (difference.TotalSeconds < 60)
            return "Just now";

        if (difference.TotalMinutes < 60)
        {
            var minutes = (int)difference.TotalMinutes;
            return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
        }

        if (difference.TotalHours < 24)
        {
            var hours = (int)difference.TotalHours;
            return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
        }

        if (difference.TotalDays < 30)
        {
            var days = (int)difference.TotalDays;
            return $"{days} day{(days == 1 ? "" : "s")} ago";
        }

        if (difference.TotalDays < 365)
        {
            var months = (int)(difference.TotalDays / 30);
            return $"{months} month{(months == 1 ? "" : "s")} ago";
        }

        var years = (int)(difference.TotalDays / 365);
        return $"{years} year{(years == 1 ? "" : "s")} ago";
    }
}