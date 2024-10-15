using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace SolidGround.Pages;

public class InputList(AppDbContext db) : PageModel
{
    public static string TurboFrameId => "inputlist";

    public Input[] Inputs { get; set; } = [];
    
    public async Task OnGetAsync()
    {
        Inputs = await db.Inputs
            .Include(i=>i.Outputs)
            .ThenInclude(o => o.Execution)
            .Include(i=>i.Files)
            .Include(i=>i.Strings)
            .ToArrayAsync();
    }
}