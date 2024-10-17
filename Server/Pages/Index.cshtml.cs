using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace SolidGround.Pages;

public class IndexModel(ILogger<IndexModel> logger, AppDbContext appDbContext) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;
    public Tag[] AllTags { get; set; }
    public Input[] Inputs { get; set; }
    
    public async Task OnGetAsync()
    {
        AllTags = await appDbContext.Tags.ToArrayAsync();
        Inputs = await appDbContext.Inputs.ToArrayAsync();
    }
}