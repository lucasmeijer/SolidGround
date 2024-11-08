using System.Text.Json.Serialization;

namespace SolidGround;

public record InputDto
{
    [JsonPropertyName("request")]
    public required RequestDto Request { get; init; }
        
    [JsonPropertyName("output")]
    public required OutputDto Output { get; init; }
}


public record RequestDto
{
    [JsonPropertyName("body_base64")]
    public required string BodyBase64 { get; init; }
        
    [JsonPropertyName("content_type")]
    public required string? ContentType { get; init; }
        
    [JsonPropertyName("route")]
    public required string Route { get; init; }
        
    [JsonPropertyName("base_path")]
    public required string BasePath { get; init; }
}

public record OutputComponentDto
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
        
    [JsonPropertyName("value")]
    public required string Value { get; init; }
}

public record StringVariableDto
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
        
    [JsonPropertyName("value")]
    public required string Value { get; init; }
}

public record AvailableVariablesDto
{
    [JsonPropertyName("string_variables")]
    public required StringVariableDto[] StringVariables { get; init; }
}
    

public record OutputDto
{
    [JsonPropertyName("string_variables")]
    public required StringVariableDto[] StringVariables { get; init; }
        
    [JsonPropertyName("output_components")]
    public required OutputComponentDto[] OutputComponents { get; init; }
}

public record RunExecutionDto
{
    [JsonPropertyName("inputs")]
    public required int[] Inputs { get; init; }
    
    [JsonPropertyName("string_variables")]
    public required StringVariableDto[] StringVariables { get; init; }

    [JsonPropertyName("endpoint")]
    public required string EndPoint { get; init; }
}

public record ExecutionStatusDto
{
    [JsonPropertyName("finished")]
    public bool Finished { get; init; }
}