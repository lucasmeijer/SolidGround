record AppState(int[] Tags, int[] Executions, string Search)
{
    public static AppState Default => new([], [-1], "");
}

record AppSnapshot(AppState State, int[] Inputs);

public record User(string Name, string HashedPassword);

public abstract record Tenant
{
    public abstract string Identifier { get; }
    public abstract User[] Users { get; }
    public abstract string ApiKey { get; }
    public abstract string BaseUrl { get; }
    public abstract string LocalBaseUrl { get; }

    public static Tenant[] All = [new FlashCardsTenant(), new SchrijfEvenMeeAssessment(), new SchrijfEvenMeeHuisArtsTenant()];
} 

record FlashCardsTenant : Tenant
{
    public override string Identifier => "flashcards";
    public override User[] Users => [new("lucas", "12324")];
    public override string ApiKey => "solidground-8a1c5cdf-3f2e-4478-b347-a4bf010a5c27";
    public override string BaseUrl => "https://flashcards.lucasmeijer.com";
    public override string LocalBaseUrl => "https://localhost:7220";
}

record SchrijfEvenMeeHuisArtsTenant : Tenant
{
    public override string Identifier => "huisarts";
    public override User[] Users => [new("lucas", "12324")];
    public override string ApiKey => "solidground-dfd4a85d-21f3-4b8d-98a9-97cd5a6b9f42";
    public override string BaseUrl => "https://huisarts.schrijfevenmee.nl";
    public override string LocalBaseUrl => "https://localhost:7172";
}

record SchrijfEvenMeeAssessment : Tenant
{
    public override string Identifier => "assessment";
    public override User[] Users => [new("lucas", "12324")];
    public override string ApiKey => "solidground-8f2e9f3d-5a1b-4c2e-9e7d-8b2c6d3f4a5b";
    public override string BaseUrl => "https://localhost:7172";
    public override string LocalBaseUrl => "https://localhost:7172";
}