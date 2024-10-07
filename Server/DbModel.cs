using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SolidGround;

using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Input> Inputs { get; set; }
    public DbSet<InputComponent> InputComponents { get; set; }
    public DbSet<Output> Outputs { get; set; }
    public DbSet<OutputComponent> OutputComponents { get; set; }
    public DbSet<Execution> Executions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Execution -> Output relationship
        modelBuilder.Entity<Execution>()
            .HasMany(e => e.Outputs)
            .WithOne(o => o.Execution)
            .HasForeignKey(o => o.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Input -> InputComponent relationship
        modelBuilder.Entity<Input>()
            .HasMany(i => i.Components)
            .WithOne(ic => ic.Input)
            .HasForeignKey(ic => ic.InputId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Output -> OutputComponent relationship
        modelBuilder.Entity<Output>()
            .HasMany(o => o.Components)
            .WithOne(oc => oc.Output)
            .HasForeignKey(oc => oc.OutputId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Output -> Input relationship
        modelBuilder.Entity<Output>()
            .HasOne(o => o.Input)
            .WithMany()
            .HasForeignKey(o => o.InputId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


public class Execution
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public string Name { get; set; }
    public List<Output> Outputs { get; set; } = new();
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
    
    public List<InputComponent> Components { get; set; } = [];
}

public class InputComponent
{
    public int Id { get; set; }

    // Foreign key to Input
    public int InputId { get; set; }
    public Input Input { get; set; }

    public ComponentType Type { get; set; }
    public string StringValue { get; set; }
    public byte[] BinaryValue { get; set; }
}

public enum ComponentType
{
    String,
    Image,
    File
}

public class Output
{
    public int Id { get; set; }

    public int ExecutionId { get; set; }
    public Execution Execution { get; set; }

    public int InputId { get; set; }
    public Input Input { get; set; }

    public ExecutionStatus Status { get; set; }
    
    public bool IsComplete { get; set; }

    public List<OutputComponent> Components { get; set; } = [];
}

public class OutputComponent
{
    public int Id { get; set; }

    public int OutputId { get; set; }
    public Output Output { get; set; }
    
    public string Name { get; set; }
    public string? Value { get; set; }
}


record InputsParams(string AppName, string Data);