using SolidGround;
using TurboFrames;

record RunExperimentTurboFrame : TurboFrame
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

    async Task<StringVariableDto[]> GetVariablesFromServiceUnderTest(IConfiguration config, HttpClient httpClient, Tenant tenant)
    {
        var requestUri = $"{tenant.BaseUrl}/solidground";
        var availableVariablesDto = await httpClient.GetFromJsonAsync<AvailableVariablesDto>(requestUri) ?? throw new Exception("No available variables found");
        
        //
        // var jdoc = await JsonDocument.ParseAsync(await result.Content.ReadAsStreamAsync());
        //
        // var d = jdoc
        //     .RootElement
        //     .EnumerateObject()
        //     .ToDictionary(k => k.Name, v => v.Value.GetString() ?? throw new InvalidOperationException());
        //
        //
        
        // if (OutputIdWhoseVariablesToUse != null)
        // {
        //     var output = await dbContext.Outputs.FindAsync(OutputIdWhoseVariablesToUse) ??
        //                  throw new BadHttpRequestException("Output " + OutputIdWhoseVariablesToUse + " not found.");
        //     await dbContext.Entry(output).Collection(o => o.StringVariables).LoadAsync();
        //     var outputStringVariables = output.StringVariables;
        //
        //     foreach (var overrideVariable in outputStringVariables)
        //         d[overrideVariable.Name] = overrideVariable.Value;
        // }

        return availableVariablesDto.StringVariables;
    }

    //static string TargetAppBaseUrl(IConfiguration config) => config.GetMandatory("SOLIDGROUND_TARGET_APP");

    public new static string TurboFrameId => "run_experiment_form";

    protected override Delegate RenderFunc =>
        async (HttpClient httpClient, IConfiguration config, Tenant tenant) =>
        {
            try
            {
                return new Html($"""                    
                                  <form 
                                     data-controller="runexperiment"
                                     class="p-4" action="{ExecutionsEndPoints.Routes.api_executions.For()}" method="post">
                                     
                                       {(await GetVariablesFromServiceUnderTest(config, httpClient, tenant)).Render(RenderVariable)} 
                                      
                                      <input type="hidden" name="baseurl" value="{tenant.BaseUrl}"/>
                                      <button type="submit" class="px-4 py-2 bg-green-200 hover:bg-green-700 rounded">
                                          Run Experiment
                                      </button>
                                      <div data-formtojson-target="errorMessage" class="error-message"></div>
                                  </form>
                                  
                                  
                                  """);
            }
            catch (HttpRequestException e)
            {
                return new($"Unable to retrieve SolidGround variables from service under test: {e.Message}");
            }
        };

    static Html RenderVariable(StringVariableDto variable) => new($"""
          <div class="mb-6">
              <label class="block text-gray-700 text-sm font-bold mb-2" for="@id">
                  {variable.Name}
              </label>
              <textarea data-controller="textarearesize"
              id="{IdFor(variable.Name)}"
              name="{IdFor(variable.Name)}"      
              class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
              rows="4"
              placeholder="{variable.Value}"
              oninput="this.classList.toggle('text-gray-500', this.value === this.placeholder)"
                  >{variable.Value}</textarea>
          </div>
          """);

    static string IdFor(string variableName) => $"SolidGroundVariable_{variableName}";
}