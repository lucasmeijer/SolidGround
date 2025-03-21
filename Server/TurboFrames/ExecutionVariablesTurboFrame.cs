using System.Web;
using SolidGround;
using TurboFrames;

record ExecutionVariablesTurboFrame(StringVariableDto[] Variables, string Name) : TurboFrame(NewExecutionDialogContentTurboFrame.TurboFrameId)
{
    protected override string LazySrc => ExecutionsEndPoints.Routes.api_executions_new.For();

    protected override Delegate RenderFunc => (Tenant tenant, IWebHostEnvironment env) => new Html($"""
         <div class="flex flex-col">
             <div class="flex items-center justify-between p-4 border-b border-gray-200 bg-gray-50 rounded-t-lg">
                 <h2 class="text-lg font-semibold text-gray-800">New Execution</h2>
                 <button onclick="this.closest('dialog').close()" 
                         class="p-2 text-gray-500 hover:text-gray-700 rounded-lg 
                                hover:bg-gray-100 transition-colors duration-200">
                     <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                         <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                     </svg>
                 </button>
             </div>
             <form 
             data-controller="runexperiment"
             class="p-4" action="{ExecutionsEndPoints.Routes.api_executions.For()}" method="post">
                 <div class="p-2 space-y-6 max-h-[calc(100vh-16rem)] overflow-y-auto bg-white text-xs">
                     {Variables.Render(RenderVariable)}
                     <input type="hidden" name="baseurl" value="{(env.IsDevelopment() ? tenant.LocalBaseUrl : tenant.BaseUrl)}"/>
                     
                 </div>
                 
                 <div class="flex items-center justify-end gap-4 p-4 border-t border-gray-200 bg-gray-50 rounded-b-lg">
                   <input 
                            type="text" 
                            id="name" 
                            name="name" 
                            placeholder="Optional execution name"
                            class="block w-64 p-3 text-md bg-white border border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 hover:border-blue-400"
                            />
                  
                     RunAmount:
                     <input 
             type="number" 
             name="runamount" 
             id="runamount"
             min="1" 
             max="10" 
             value="1"
             class="block w-32 p-3 text-md bg-white border border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 hover:border-blue-400"
         />
                     
                     <button type="button" data-action="runexperiment#cancel" 
                             class="px-4 py-2 text-gray-700 bg-gray-100 rounded-lg 
                                    hover:bg-gray-200 transition-colors duration-200 
                                    focus:outline-none focus:ring-2 focus:ring-gray-400">
                         Cancel
                     </button>
                     
                     <button type="submit" class="px-4 py-2 text-white bg-blue-600 rounded-lg 
                                    hover:bg-blue-700 transition-colors duration-200 
                                    focus:outline-none focus:ring-2 focus:ring-blue-500">
                         Execute on X inputs
                     </button>
                 </div>
                 
                 <div data-formtojson-target="errorMessage" class="error-message"></div>
             </form>
         </div>
         """);

    static Html RenderVariable(StringVariableDto variable) =>
        new($"""
             <div class="mb-6">
                 <label class="block text-gray-700 text-sm font-bold mb-2" for="@id">
                     {variable.Name}
                 </label>
                 {(variable.Options == null ? RenderTextAreaFor(variable) : RenderDropdownFor(variable))}                
             </div>
             """);

    static Html RenderTextAreaFor(StringVariableDto variable)
    {
        var htmlEncode = HttpUtility.HtmlEncode(variable.Value);
        return new($"""
                    <textarea data-controller="textarearesize"
                        id="{IdFor(variable.Name)}"
                        name="{IdFor(variable.Name)}"      
                        class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
                        rows="4"
                        placeholder="{htmlEncode}"
                        oninput="this.classList.toggle('text-gray-500', this.value === this.placeholder)"
                            >{htmlEncode}</textarea>
                    """);
    }

    static Html RenderDropdownFor(StringVariableDto variable) => new($"""
                                                                      <select
                                                                      id="{IdFor(variable.Name)}"
                                                                      name="{IdFor(variable.Name)}"
                                                                      class="block w-32 p-3 text-md bg-white border border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 cursor-pointer hover:border-blue-400"
                                                                         >
                                                                         {variable.Options!.Render(o => RenderOption(o, o == variable.Value))}
                                                                      </select>
                                                                      """);

    static Html RenderOption(string o, bool selected) => new($"<option value='{o}' {(selected?"selected":"")}>{o}</option>");

    static string IdFor(string variableName) => $"SolidGroundVariable_{variableName}";
}