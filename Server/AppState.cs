record AppState(int[] Tags, int[] Executions, string Search)
{
    public static AppState Default => new([], [-1], "");
}

record AppSnapshot(AppState State, int[] Inputs);


record User(string Name, string HashedPassword);

abstract record Tenant
{
    public abstract string Identifier { get; }
    public abstract User[] Users { get; }
    public abstract string ApiKey { get; }
    public abstract string BaseUrl { get; }
} 

record FlashCardsTenant : Tenant
{
    public static readonly string _ApiKey = "solidground-8a1c5cdf-3f2e-4478-b347-a4bf010a5c27";
    public override string Identifier => "flashcards";
    public override User[] Users => [new("lucas", "12324")];
    public override string ApiKey => _ApiKey;
    public override string BaseUrl => "https://localhost:7220";
}