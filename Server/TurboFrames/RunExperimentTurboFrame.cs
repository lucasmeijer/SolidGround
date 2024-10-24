using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SolidGround;
using TurboFrames;


public record RunExperimentTurboFrame : TurboFrame
{
    int? OutputIdWhoseVariablesToUse { get; }
    
    public RunExperimentTurboFrame(int outputIdWhoseVariablesToUse) : base(TurboFrameId) => OutputIdWhoseVariablesToUse = outputIdWhoseVariablesToUse;

    public RunExperimentTurboFrame() : base(TurboFrameId)
    {
    }

    protected override string LazySrc => Route;

    string Route => OutputIdWhoseVariablesToUse == null
        ? ExperimentEndPoints.Routes.api_experiment_newform
        : ExperimentEndPoints.Routes.api_experiment_newform_id.For(OutputIdWhoseVariablesToUse.Value);

    async Task<KeyValuePair<string, string>[]> GetVariablesFromServiceUnderTest(IConfiguration config, HttpClient httpClient, AppDbContext dbContext)
    {
        var requestUri = $"{config.GetMandatory("SOLIDGROUND_TARGET_APP")}/solidground";
        var result = await httpClient.GetAsync(requestUri);
        result.EnsureSuccessStatusCode();

        var jdoc = await JsonDocument.ParseAsync(await result.Content.ReadAsStreamAsync());

        var d = jdoc
            .RootElement
            .EnumerateObject()
            .ToDictionary(k => k.Name, v => v.Value.GetString() ?? throw new InvalidOperationException());

        if (OutputIdWhoseVariablesToUse != null)
        {
            var output = await dbContext.Outputs.FindAsync(OutputIdWhoseVariablesToUse) ??
                         throw new BadHttpRequestException("Output " + OutputIdWhoseVariablesToUse + " not found.");
            await dbContext.Entry(output).Collection(o => o.StringVariables).LoadAsync();
            var outputStringVariables = output.StringVariables;

            foreach (var overrideVariable in outputStringVariables)
                d[overrideVariable.Name] = overrideVariable.Value;
        }

        return d.ToArray();
    }

    public new static string TurboFrameId => "run_experiment_form";

    protected override Delegate RenderFunc =>
        async (HttpClient httpClient, AppDbContext appDbContext, IConfiguration config) =>
        {
            try
            {
                return new Html($"""                    
                                 <form class="p-4" action="{ExperimentEndPoints.Routes.api_experiment.For()}" method="post">
                                      {(await GetVariablesFromServiceUnderTest(config, httpClient, appDbContext)).Render(RenderVariable)} 
                                     <input type="hidden" name="ids" value="[1,2,3]">
                                     <input type="hidden" name="name" value="Lucas!">
                                     <button type="submit" class="px-4 py-2 bg-green-200 hover:bg-green-700 rounded">
                                         Run Experiment
                                     </button>
                                 </form>
                                 """);
            }
            catch (HttpRequestException e)
            {
                return new($"Unable to retrieve SolidGround variables from service under test: {e.Message}");
            }
        };

    static Html RenderVariable(KeyValuePair<string, string> variable) => new($"""
          <div class="mb-6">
              <label class="block text-gray-700 text-sm font-bold mb-2" for="@id">
                  {variable.Key}
              </label>
              <textarea
              id="{IdFor(variable)}"
              name="{IdFor(variable)}"      
              class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
              rows="4"
              placeholder="{variable.Value}"
              oninput="this.classList.toggle('text-gray-500', this.value === this.placeholder)"
                  >{variable.Value}</textarea>
          </div>
          """);

    static string IdFor(KeyValuePair<string, string> variable) => $"SolidGroundVariable_{variable.Key}";
}