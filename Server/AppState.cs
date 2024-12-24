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

    public static Tenant[] All = [
        new FlashCardsTenant(), 
        new SchrijfEvenMeeAssessmentTenant(), 
        new SchrijfEvenMeeHuisArtsTenant(),
        new SchrijfEvenVanillaTenant(),
        new SchrijfEvenMeeGgzTenant(),
        new SchrijfEvenMeeScintillaTenant(),
        new SchrijfEvenMeeChristinaTenant()
    ];
} 

record FlashCardsTenant : Tenant
{
    public override string Identifier => "flashcards";
    public override User[] Users => [new("lucas", "12324")];
    public override string ApiKey => "solidground-8a1c5cdf-3f2e-4478-b347-a4bf010a5c27";
    public override string BaseUrl => "https://flashcards.lucasmeijer.com";
    public override string LocalBaseUrl => "https://localhost:7220";
}

record SchrijfEvenMeeHuisArtsTenant : SchrijfEvenMeeTenant
{
    public override string Identifier => "huisarts";
    public override string ApiKey => "solidground-dfd4a85d-21f3-4b8d-98a9-97cd5a6b9f42";
}

abstract record SchrijfEvenMeeTenant : Tenant
{
    public override User[] Users => [new("lucas", "12324")];
    public override string BaseUrl => $"https://{Identifier}.schrijfevenmee.nl";
    public override string LocalBaseUrl => "https://localhost:7172";
}

record SchrijfEvenMeeAssessmentTenant : SchrijfEvenMeeTenant
{
    public override string Identifier => "assessment";
    public override string ApiKey => "solidground-8f2e9f3d-5a1b-4c2e-9e7d-8b2c6d3f4a5b";
}

record SchrijfEvenMeeChristinaTenant : SchrijfEvenMeeTenant
{
    public override string Identifier => "christina";
    public override string ApiKey => "solidground-1f4c7b9a-d5e2-4f8c-b6a3-9d7e5f2c8b4a";
}

record SchrijfEvenVanillaTenant : SchrijfEvenMeeTenant
{
    public override string BaseUrl => "https://schrijfevenmee.nl";
    public override string Identifier => "vanilla";
    public override string ApiKey => "solidground-2d8e5f1c-b7a9-4e6f-8c3b-2d9a5f7e4b1c";
}

record SchrijfEvenMeeScintillaTenant : SchrijfEvenMeeTenant
{
    public override string Identifier => "scintilla";
    public override string ApiKey => "solidground-9f3c1d7a-e5b2-4f8c-b6a4-2d8e5f1c7b9a";
}

record SchrijfEvenMeeGgzTenant : SchrijfEvenMeeTenant
{
    public override string Identifier => "ggz";
    public override string ApiKey => "solidground-8c4b2d9a-f5e1-4d7c-b3a6-9f2e8d4c7b1a";
}

record SchrijfEvenMeeBlauwbertTenant : SchrijfEvenMeeTenant
{
    public override string Identifier => "blauwberg";
    public override string ApiKey => "solidground-1f3e8d7b-a2c4-4f9d-95e8-6b7a2c1d9f3e";
}