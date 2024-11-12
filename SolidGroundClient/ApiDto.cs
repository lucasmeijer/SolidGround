using System.Text.Json.Serialization;

namespace SolidGround;

public record InputDto
{
    [JsonPropertyName("request")]
    public required RequestDto Request { get; init; }
    
    [JsonPropertyName("tag_names")]
    public required string[] TagNames { get; init; }
    
    [JsonPropertyName("output")]
    public required OutputDto Output { get; init; }

    [JsonPropertyName("name")]
    public required string? Name { get; init; }
}


public record RequestDto
{
    [JsonPropertyName("body_base64")]
    public required string BodyBase64 { get; init; }
        
    [JsonPropertyName("content_type")]
    public required string? ContentType { get; init; }
        
    [JsonPropertyName("route")]
    public required string Route { get; init; }
    
    [JsonPropertyName("query_string")]
    public required string? QueryString { get; init; }
    
    [JsonPropertyName("method")]
    public required string Method { get; init; }
}

public record OutputComponentDto
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
        
    [JsonPropertyName("value")]
    public required string Value { get; init; }
    
    [JsonPropertyName("content_type")]
    public required string ContentType { get; init; }
}

public record StringVariableDto
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
        
    [JsonPropertyName("value")]
    public required string Value { get; init; }
    
    [JsonPropertyName("options")]
    public required string[] Options { get; init; }
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

    [JsonPropertyName("baseurl")]
    public required string BaseUrl { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("runamount")]
    public required int RunAmount { get; set; }
}

public record ExecutionStatusDto
{
    [JsonPropertyName("finished")]
    public bool Finished { get; init; }
}