using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using SolidGround;

namespace SolidGround.Pages
{
    public class ExecutionDetailsModel(AppDbContext context) : PageModel
    {
        // public Execution Execution { get; set; }
        // public IQueryable<Execution> AllReferences { get; set; }

        public Input[] Inputs { get; private set; }
        public Tag[] Tags { get; private set; }
        
        
        public async Task<IActionResult> OnGetAsync(int id)
        {
            Inputs = await context.Inputs
                .Include(i=>i.Outputs)
                .ThenInclude(o => o.Execution)
                
                .Include(i=>i.Outputs)
                .ThenInclude(o => o.Components)
                
                .Include(i => i.Strings)
                .Include(i => i.Files)
                
                .ToArrayAsync();
            
            Tags = [..await context.Tags.ToArrayAsync(), new Tag() { Name = "Dummy1"}, new Tag(){ Name = "Dummy2"}];
            
            return Page();
        }

        public Output? FindOutput(Execution referenceExecution, Output output)
        {
            return referenceExecution.Outputs.FirstOrDefault(o => o.InputId == output.InputId);
        }
    }
}

static class Extensions
{
    public static IEnumerable<OutputComponent> PutResultFirst(this ICollection<OutputComponent> self)
    {
        var result = self.FirstOrDefault(o => o.Name == "result");
        if (result != null)
            yield return result;
        foreach (var o in self)
        {
            if (o != result)
                yield return o;
        }
    }
}