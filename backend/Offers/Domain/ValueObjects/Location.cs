namespace Offers.Domain.ValueObjects;

/// <summary>
/// Value object representing the location of a vehicle.
/// Stored as JSONB in PostgreSQL.
/// </summary>
public record Location
{
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string Country { get; init; } = "USA";

    public Location() { }

    public Location(string city, string state, string zipCode, string country = "USA")
    {
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }
}
