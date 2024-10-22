using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TurboFrames;

namespace SolidGround;

[ApiController]
[Route("/api/output/{id:int}")]
public class OutputController(AppDbContext db) : ControllerBase
{
    [HttpDelete]
    public async Task<IActionResult> OnDelete(int id)
    {
        var obj = await db.Outputs.FindAsync(id);
        if (obj == null)
            return NotFound();
        
        db.Outputs.Remove(obj);
        await db.SaveChangesAsync();

        return new TurboStream("remove", Target: OutputTurboFrame2.TurboFrameIdFor(id));
    }

    [HttpPost]
    public async Task<IActionResult> OnPost(int id, HttpRequest request)
    {
        var jsonDoc = await JsonDocument.ParseAsync(request.Body);
    
        var output = await db.Outputs.FindAsync(id);
        if (output == null)
            return NotFound($"Output {id} not found");

        if (!jsonDoc.RootElement.TryGetProperty("outputs", out var outputElement))
            return BadRequest("output element not found");
    
        output.Components = InputController.OutputComponentsFromJsonElement(outputElement);
        output.Status = ExecutionStatus.Completed;
        await db.SaveChangesAsync();
        return Ok();    
    }
}