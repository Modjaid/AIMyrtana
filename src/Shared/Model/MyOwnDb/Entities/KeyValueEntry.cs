namespace MyOwnDb.Entities;

public sealed class KeyValueEntry
{
    public long Id { get; set; }

    public required string Key { get; set; }

    public string? Value { get; set; }
}

