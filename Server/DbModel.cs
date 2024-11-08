using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SolidGround;

class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Input> Inputs { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<InputString> InputStrings { get; set; }
    public DbSet<InputFile> InputFiles { get; set; }
    public DbSet<Output> Outputs { get; set; }
    public DbSet<OutputComponent> OutputComponents { get; set; }
    public DbSet<Execution> Executions { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings => 
            warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>()
            .HasMany(i => i.Inputs)
            .WithMany(i => i.Tags)
            .UsingEntity(j => j.ToTable("InputSetInputs"));
    }
}



class Execution
{
    public int Id { get; [UsedImplicitly] set; }
    public DateTime StartTime { get; [UsedImplicitly] set; }
    public bool IsReference { get; [UsedImplicitly] set; }
    public List<Output> Outputs { get; [UsedImplicitly] set; } = [];
    [MaxLength(200)]
    public string? Name { get; set; } = null;

    public bool SolidGroundInitiated { get; [UsedImplicitly] set; }

    public Input Input { get; [UsedImplicitly] set; } = null!;
    public int InputId { get; [UsedImplicitly] set; }
    
    [UsedImplicitly]
    public List<StringVariable> StringVariables { get; set; } = [];
}

class Tag
{
    [UsedImplicitly] public int Id { get; set; }
    [MaxLength(200)]
    [UsedImplicitly] public string Name { get; set; } = null!;
    [UsedImplicitly] public List<Input> Inputs { get; set; } = [];
}

public enum ExecutionStatus
{
    Started,
    Completed,
    Failed
}

// ReSharper disable InconsistentNaming
class Input
{
    
    public int Id { get; [UsedImplicitly] set; }
    [MaxLength(200)] public string? Name { get; set; }
    public List<Tag> Tags { get; [UsedImplicitly] set; } = [];
    public List<InputString> Strings { get; [UsedImplicitly] set; } = [];
    public List<InputFile> Files { get; [UsedImplicitly] set; } = [];
    [MaxLength(200)]
    public string OriginalRequest_Route { get; [UsedImplicitly] set; } = null!;
    
    [SuppressMessage("ReSharper", "EntityFramework.ModelValidation.UnlimitedStringLength")]
    public string? OriginalRequest_QueryString { get; [UsedImplicitly] set; }
    
    public List<Output> Outputs { get; [UsedImplicitly] set; } = [];
    [MaxLength(200)]
    public string? OriginalRequest_ContentType { get; [UsedImplicitly] set; }

    [MaxLength(10)]
    public string OriginalRequest_Method { get; [UsedImplicitly] set; } = "POST";
    
    [SuppressMessage("ReSharper", "EntityFramework.ModelValidation.UnlimitedStringLength")]
    public string OriginalRequest_Body { get; [UsedImplicitly] set; } = null!;
    
    public static Input Example => new()
    {
        Id = 1,
        Name = "Our dummy input",
        Files = [new InputFile() { Name = "input.png", Bytes = [0], Id = 0, MimeType = "image/png" }],
        Strings = [new InputString() { Name = "language", Value = "nl"}],
        Outputs = [
            Output.Example
        ]
    };
}
// ReSharper restore InconsistentNaming


class InputString
{
    public int Id { get; set; }

    public int InputId { get; set; }
    public Input Input { get; set; } = null!;

    public string Name { get; set; } = null!;
    
    public int Index { get; set; }
    public string Value { get; set; } = null!;
}


class InputFile
{
    public int Id { get; set; }

    public int InputId { get; [UsedImplicitly] set; }

    [MaxLength(200)]
    public string Name { get; [UsedImplicitly] set; } = null!;
    
    public int Index { get; [UsedImplicitly] set; }
    [MaxLength(100)]
    public string MimeType { get; [UsedImplicitly] set; } = null!;
    public byte[] Bytes { get; [UsedImplicitly] set; } = [];
}

class Output
{
    public int Id { get; [UsedImplicitly] set; }

    public int ExecutionId { get; [UsedImplicitly] set; }
    public Execution Execution { get; [UsedImplicitly] set; } = null!;

    public int InputId { get; [UsedImplicitly] set; }
    public Input Input { get; [UsedImplicitly] set; }  = null!;
    
    public ExecutionStatus Status { get; set; }
    public List<OutputComponent> Components { get; set; } = [];
    
    public List<StringVariable> StringVariables { get; set; } = [];
    
    public string TurboFrameId => $"output_{Id}";
    
    public static Output Example => new()
    {
        Status = ExecutionStatus.Completed,
        Components = [new OutputComponent() { Name = "fullresponse.txt", Value = "heel lange output" }],
    };
}

class StringVariable
{
    public int? OutputId { get; set; }
    public int? ExecutionId { get; set; }
    public int Id { get; set; }
    [MaxLength(200)]
    public string Name { get; [UsedImplicitly] set; } = null!;
    
    [SuppressMessage("ReSharper", "EntityFramework.ModelValidation.UnlimitedStringLength")]
    public string Value { get; [UsedImplicitly] set; } = null!;
}

class OutputComponent
{
    public int Id { get; [UsedImplicitly] set; }

    public int OutputId { get; [UsedImplicitly] set; }
    public Output Output { get; [UsedImplicitly] set; } = null!;
    
    [MaxLength(200)]
    public string Name { get; [UsedImplicitly] set; } = null!;
    [SuppressMessage("ReSharper", "EntityFramework.ModelValidation.UnlimitedStringLength")]
    public string? Value { get; [UsedImplicitly] set; }
}
