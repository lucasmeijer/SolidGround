using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.EntityFrameworkCore;
using SolidGround;
using SolidGround.Pages;

namespace SolidGround.Pages;

public record IndexPage() : SolidGroundPage("SolidGround")
{
    protected override async Task<Html> RenderBodyContent(IServiceProvider serviceProvider)
    {
        var appDbContext = serviceProvider.GetRequiredService<AppDbContext>();
        
        var allInputsIdsFrom = await appDbContext.Inputs.Select(i => i.Id).ToArrayAsync();
        var allExecutionIds = await appDbContext.Executions.Select(e=>e.Id).ToArrayAsync();
        
        return new($"""
                    <div class="m-5 flex flex-col gap-4">
                       {await new FilterBarTurboFrame([]).RenderAsync(serviceProvider)}
                       {await new InputListTurboFrame(allInputsIdsFrom, allExecutionIds).RenderAsync(serviceProvider)}
                    </div>
                    """);
    }
}