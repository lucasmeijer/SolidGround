using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace SolidGround.Pages
{
    public class ExecutionDetailsModel(AppDbContext context) : PageModel
    {
        // public Execution Execution { get; set; }
        // public IQueryable<Execution> AllReferences { get; set; }

        public Input[] Inputs { get; private set; }
        
        public async Task<IActionResult> OnGetAsync(int id)
        {
            Inputs = context.Inputs
                .Include(i=>i.Outputs)
                .ThenInclude(o => o.Execution)
                
                .Include(i=>i.Outputs)
                .ThenInclude(o => o.Components)
                
                .Include(i => i.Strings)
                .Include(i => i.Files)
                
                .ToArray();
            
            //
            // var execution = await context.Executions
            //     .Include(e => e.Outputs)
            //         .ThenInclude(o => o.Input)
            //             .ThenInclude(i => i.Strings)
            //     .Include(e => e.Outputs)
            //         .ThenInclude(o => o.Input)
            //             .ThenInclude(i => i.Files)
            //     .Include(e => e.Outputs)
            //         .ThenInclude(o => o.Components)
            //     .FirstOrDefaultAsync(e => e.Id == id);
            //
            // AllReferences = context.Executions
            //     .Where(e => e.IsReference && e != execution)
            //     .Include(e => e.Outputs)
            //     .ThenInclude(o => o.Components);
            //
            // if (execution == null)
            //     return NotFound();
            //
            // Execution = execution;

            return Page();
        }

        public Output? FindOutput(Execution referenceExecution, Output output)
        {
            return referenceExecution.Outputs.FirstOrDefault(o => o.InputId == output.InputId);
        }
    }
}