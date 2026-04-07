namespace MyOwnDb;

public sealed class MyOwnDbOptions
{
    public const string SectionName = "MyOwnDb";

    public string ConnectionString { get; set; } = "";
}

