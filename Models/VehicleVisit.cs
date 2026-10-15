using SQLite;

namespace ParqueaderoMoto.Models;

public class VehicleVisit
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string Plate { get; set; } = "";

    public string CustomerName { get; set; } = "";
    public int ParkingSpace { get; set; }
    public DateTime EntryAt { get; set; }
    public DateTime? ExitAt { get; set; }
    public long AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = "";
    [Ignore]
    public bool IsActive => ExitAt is null;
}
