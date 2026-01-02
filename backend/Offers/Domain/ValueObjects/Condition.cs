namespace Offers.Domain.ValueObjects;

/// <summary>
/// Value object representing the condition of a vehicle.
/// Stored as JSONB in PostgreSQL.
/// </summary>
public record Condition
{
    /// <summary>
    /// Vehicle mileage in miles.
    /// </summary>
    public int Mileage { get; init; }

    /// <summary>
    /// Exterior condition rating (Excellent, Good, Fair, Poor).
    /// </summary>
    public string Exterior { get; init; } = string.Empty;

    /// <summary>
    /// Interior condition rating (Excellent, Good, Fair, Poor).
    /// </summary>
    public string Interior { get; init; } = string.Empty;

    /// <summary>
    /// Mechanical condition rating (Excellent, Good, Fair, Poor).
    /// </summary>
    public string Mechanical { get; init; } = string.Empty;

    public Condition() { }

    public Condition(int mileage, string exterior, string interior, string mechanical)
    {
        Mileage = mileage;
        Exterior = exterior;
        Interior = interior;
        Mechanical = mechanical;
    }
}
