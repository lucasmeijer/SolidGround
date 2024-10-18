using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using SolidGround;
using TurboFrames;


[Route(Route)]
[Route($"{Route}/{{outputIdWhoseVariablesToUse}}")]
public record RunExperimentTurboFrame : TurboFrame
{
    const string Route = "/runexperiment";
    
    public int? OutputIdWhoseVariablesToUse { get; }
    
    public RunExperimentTurboFrame(int outputIdWhoseVariablesToUse) : base(TurboFrameId) => OutputIdWhoseVariablesToUse = outputIdWhoseVariablesToUse;

    public RunExperimentTurboFrame() : base(TurboFrameId)
    {
    }
    
    public LazyFrame Lazy => new(base.TurboFrameId, RouteFor(OutputIdWhoseVariablesToUse));

    public static string RouteFor(int? outputId) => outputId == null ? Route : $"{Route}/{outputId}";

    public record Model(KeyValuePair<string, string>[] Values) : TurboFrameModel;
    
    protected override async Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var httpClient = serviceProvider.GetRequiredService<HttpClient>();
        
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
            var output = await dbContext.Outputs.FindAsync(OutputIdWhoseVariablesToUse) ?? throw new BadHttpRequestException("Output " + OutputIdWhoseVariablesToUse + " not found.");
            await dbContext.Entry(output).Collection(o=>o.StringVariables).LoadAsync();
            var outputStringVariables = output.StringVariables;
            
            foreach (var overrideVariable in outputStringVariables)
                d[overrideVariable.Name] = overrideVariable.Value;
        }

        return new Model(d.ToArray());
    }

    static void test()
    {
        ParseExpression(() => new RunExperimentTurboFrame(23));
        
        void ParseExpression(Expression e)
        {
            
        }
    }

    public new static string TurboFrameId => "run_experiment_form";

    public void Deconstruct(out int? output_id_whose_variables_to_use)
    {
        output_id_whose_variables_to_use = this.OutputIdWhoseVariablesToUse;
    }
}