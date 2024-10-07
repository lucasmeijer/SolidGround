using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace SolidGround.Pages
{
    public class ExecutionDetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public Execution Execution { get; set; }

        public ExecutionDetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var execution = await _context.Executions
                .Include(e => e.Outputs)
                .ThenInclude(o => o.Input)
                .ThenInclude(i => i.Components)
                .Include(e => e.Outputs)
                .ThenInclude(o => o.Components)
                .FirstOrDefaultAsync(e => e.Id == id);
            
            if (execution == null)
                return NotFound();
            
            Execution = execution;

            return Page();
        }
    }
}