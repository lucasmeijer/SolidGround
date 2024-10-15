using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SolidGround.Pages;

public class RunExperimentForm(HttpClient httpClient) : PageModel
{
    public KeyValuePair<string, string>[] _variables = [];

    public async Task OnGetAsync()
    {
        var result = await httpClient.GetAsync("https://localhost:7220/solidground");
        result.EnsureSuccessStatusCode();
        var jdoc = await JsonDocument.ParseAsync(await result.Content.ReadAsStreamAsync());
        _variables = jdoc.RootElement.EnumerateObject().Select(p => new KeyValuePair<string, string>(p.Name, p.Value.GetString() ?? throw new InvalidOperationException())).ToArray();
    }
}