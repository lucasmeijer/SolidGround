using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace SolidGround.Pages;

public class OutputModel(AppDbContext appDbContext) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    public Output Output { get; set; } = null!;
    
    public async Task OnGetAsync()
    {
        Output = await appDbContext.Outputs
            .Include(i => i.Components)
            .Include(o => o.Execution)
            .FirstOrDefaultAsync(o => o.Id == Id) ?? throw new BadHttpRequestException("Output not found");

    }
}