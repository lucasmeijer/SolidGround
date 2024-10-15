using System.Collections;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace SolidGround;

using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Input> Inputs { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<InputString> InputStrings { get; set; }
    public DbSet<InputFile> InputFiles { get; set; }
    public DbSet<Output> Outputs { get; set; }
    public DbSet<OutputComponent> OutputComponents { get; set; }
    public DbSet<Execution> Executions { get; set; }

    public IIncludableQueryable<Input, List<InputString>> CompleteInputs => this.Inputs
        .Include(i => i.Outputs)
        .ThenInclude(o => o.Execution)
        .Include(i => i.Outputs)
        .ThenInclude(o => o.Components)
        .Include(i => i.Outputs)
        .ThenInclude(o => o.StringVariables)
        .Include(i => i.Files)
        .Include(i => i.Strings);
        
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Execution -> Output relationship
        modelBuilder.Entity<Execution>()
            .HasMany(e => e.Outputs)
            .WithOne(o => o.Execution)
            .HasForeignKey(o => o.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Tag>()
            .HasMany(i => i.Inputs)
            .WithMany(i => i.Tags)
            .UsingEntity(j => j.ToTable("InputSetInputs"));
        
        // Configure Input -> InputStrings relationship
        modelBuilder.Entity<Input>()
            .HasMany(i => i.Strings)
            .WithOne(ic => ic.Input)
            .HasForeignKey(ic => ic.InputId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Input>()
            .HasMany(i => i.Outputs)
            .WithOne(ic => ic.Input)
            .HasForeignKey(ic => ic.InputId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure Input -> InputFiles relationship
        modelBuilder.Entity<Input>()
            .HasMany(i => i.Files)
            .WithOne(ic => ic.Input)
            .HasForeignKey(ic => ic.InputId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Output -> OutputComponent relationship
        modelBuilder.Entity<Output>()
            .HasMany(o => o.Components)
            .WithOne(oc => oc.Output)
            .HasForeignKey(oc => oc.OutputId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure Output -> StringVariable relationship
        modelBuilder.Entity<Output>()
            .HasMany(o => o.StringVariables)
            .WithOne(oc => oc.Output)
            .HasForeignKey(oc => oc.OutputId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


public class Execution
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsReference { get; set; }
    public List<Output> Outputs { get; set; } = [];
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Input> Inputs { get; set; } = [];
}

public enum ExecutionStatus
{
    Started,
    Completed,
    Failed
}

public class Input
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<Tag> Tags { get; set; } = [];
    public List<InputString> Strings { get; set; } = [];
    public List<InputFile> Files { get; set; } = [];
    public string OriginalRequest_Route { get; set; }
    public List<Output> Outputs { get; set; } = [];
    public string OriginalRequest_ContentType { get; set; }
    public string OriginalRequest_Body { get; set; }
    public string OriginalRequest_Host { get; set; }
    public string? TurboFrameId => $"input_{Id}";

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



public class InputString
{
    public int Id { get; set; }

    public int InputId { get; set; }
    public Input Input { get; set; }

    public string Name { get; set; }
    
    public int Index { get; set; }
    public string Value { get; set; }
}

public class InputFile
{
    public int Id { get; set; }

    // Foreign key to Input
    public int InputId { get; set; }
    public Input Input { get; set; }

    public string Name { get; set; }
    
    public int Index { get; set; }
    public string MimeType { get; set; }
    public byte[] Bytes { get; set; }
}

public class Output
{
    public int Id { get; set; }

    public int ExecutionId { get; set; }
    public Execution Execution { get; set; }

    public int InputId { get; set; }
    public Input Input { get; set; }
    
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

public class StringVariable
{
    public Output Output { get; set; }
    public int OutputId { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
}

public class OutputComponent
{
    public int Id { get; set; }

    public int OutputId { get; set; }
    public Output Output { get; set; }
    
    public string Name { get; set; }
    public string? Value { get; set; }
}
