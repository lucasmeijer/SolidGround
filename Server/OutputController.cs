using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace SolidGround;

[ApiController]
[Route("/api/output")]
public class OutputController(AppDbContext db) : ControllerBase
{
    [HttpDelete("{id:int}")]
    public async Task<IResult> OnDelete(int id)
    {
        var obj = await db.Outputs.FindAsync(id);
        if (obj == null)
            return Results.BadRequest();
    
        db.Outputs.Remove(obj);
        await db.SaveChangesAsync();
        return Results.Content($"<turbo-stream action=\"remove\" target=\"output_{id}\"></turbo-stream>", "text/vnd.turbo-stream.html");    
    }

    [HttpPost("{id:int}")]
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