using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SolidGround;
using TurboFrames;


[Route(Route)]
[Route($"{Route}/{{outputIdWhoseVariablesToUse}}")]
public record RunExperimentTurboFrame2 : TurboFrame2
{
    const string Route = "/runexperiment";
    
    public int? OutputIdWhoseVariablesToUse { get; }
    
    public RunExperimentTurboFrame2(int outputIdWhoseVariablesToUse) : base(TurboFrameId) => OutputIdWhoseVariablesToUse = outputIdWhoseVariablesToUse;

    public RunExperimentTurboFrame2() : base(TurboFrameId)
    {
    }

    protected override string LazySrc => Route;

    public static string RouteFor(int? outputId) => outputId == null ? Route : $"{Route}/{outputId}";

    record Model(KeyValuePair<string, string>[] Values) : TurboFrameModel;

    async Task<Model> BuildModel(IConfiguration config, HttpClient httpClient, AppDbContext dbContext)
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

        return new(d.ToArray());
    }

    public new static string TurboFrameId => "run_experiment_form";
    
    protected override async Task<Html> RenderAsync(IServiceProvider serviceProvider)
    {
        var model = await BuildModel(serviceProvider.GetRequiredService<IConfiguration>(),
            serviceProvider.GetRequiredService<HttpClient>(),
            serviceProvider.GetRequiredService<AppDbContext>());

        Html RenderVariable(KeyValuePair<string, string> variable) => new($"""
           <div class="mb-6">
               <label class="block text-gray-700 text-sm font-bold mb-2" for="@id">
                   {variable.Key}
               </label>
               <textarea
               id="{IdFor(variable)}"
               name="{IdFor(variable)}"      
               class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
               rows="4"
               placeholder="@variable.Value"
               oninput="this.classList.toggle('text-gray-500', this.value === this.placeholder)"
                   >{variable.Value}</textarea>
           </div>
           """);

        string IdFor(KeyValuePair<string, string> variable) => $"SolidGroundVariable_{variable.Key}";

        return new($"""                    
                   <form class="p-4" action="/api/experiment" method="post">
                        {model.Values.Render(RenderVariable)} 
                       <input type="hidden" name="ids" value="[1,2,3]">
                       <input type="hidden" name="name" value="Lucas!">
                       <button type="submit" class="px-4 py-2 bg-green-200 hover:bg-green-700 rounded">
                           Run Experiment
                       </button>
                   </form>
                   """);
    }

}