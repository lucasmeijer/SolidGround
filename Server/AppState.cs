public record AppState(int[] Tags, int[] Executions, string Search)
{
    public static AppState Default => new([], [-1], "");
}

public record AppSnapshot(AppState State, int[] Inputs);


record User(string Name, string HashedPassword);

abstract record Tenant
{
    public abstract string Identifier { get; }
    public abstract User[] Users { get; }
    public abstract string ApiKey { get; }
} 

record FlashCardsTenant : Tenant
{
    public static readonly string _ApiKey = "54983894579837459837495384";
    public override string Identifier => "flashcards";
    public override User[] Users => [new("lucas", "12324")];
    public override string ApiKey => _ApiKey;
}