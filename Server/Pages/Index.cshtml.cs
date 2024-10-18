using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace SolidGround.Pages;

public class IndexModel(AppDbContext appDbContext) : PageModel
{
    public int[] InputIds { get; set; }
    
    public async Task OnGetAsync()
    {
        InputIds = await appDbContext.Inputs.Select(i=>i.Id).ToArrayAsync();
    }
}