using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.EntityFrameworkCore;
using SolidGround;
using SolidGround.Pages;

namespace SolidGround.Pages;

public record IndexPage() : SolidGroundPage("SolidGround")
{
    protected override async Task<Html> RenderBodyContent(IServiceProvider serviceProvider) => new($"""
         <div class="m-5 flex flex-col gap-4">
            <details class="bg-white shadow-md rounded-lg group">
                <summary class="p-4 cursor-pointer flex justify-between items-center bg-green-100 rounded-lg">
                    <h3 class="font-semibold">Run Experiment</h3>
                    <svg class="w-5 h-5 transition-transform duration-200 group-open:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                    </svg>
                </summary>
                
                <div class="p-4">
                    {new RunExperimentTurboFrame().RenderLazy()}
                </div>
            </details>
            
            {await new FilterBarTurboFrame([]).RenderAsync(serviceProvider)}
            {await new InputListTurboFrame(await AllInputsIdsFrom(serviceProvider)).RenderAsync(serviceProvider)}
         </div>
         """);

    static async Task<int[]> AllInputsIdsFrom(IServiceProvider serviceProvider) =>
        await serviceProvider.GetRequiredService<AppDbContext>()
            .Inputs
            .Select(i => i.Id)
            .ToArrayAsync();
}