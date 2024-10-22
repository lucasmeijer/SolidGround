using JetBrains.Annotations;
using Microsoft.AspNetCore.Html;

namespace SolidGround.Pages;

public static class Buttons
{
    public static string Attrs => "inline-flex text-white font-semibold rounded-lg shadow-md px-4 py-2 h-16 focus:outline-none focus:ring-2 focus:ring-opacity-75 items-center justify-center flex-none";
    public static string RedAttrs => "bg-red-500 hover:bg-red-700 focus:ring-red-400";
    public static string GreenAttrs => "bg-green-500 hover:bg-green-700 focus:ring-green-400";
    
    public static Html RemoveButton(string color) => new(
        $"""
             <button type="submit" class="text-{color}-400 hover:text-{color}-600 focus:outline-none">
                  <svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                      <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/>
                  </svg>
              </button>
             """);
}