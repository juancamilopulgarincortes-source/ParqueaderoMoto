using ParqueaderoMoto.Models;
using SQLite;

namespace ParqueaderoMoto.Services;

public sealed class ParkingDatabase
{
    private readonly SQLiteAsyncConnection _db;
    private const int Capacity = 20;

    public ParkingDatabase()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "parqueadero.db3");
        _db = new SQLiteAsyncConnection(path);
        _db.CreateTableAsync<VehicleVisit>().Wait();
    }

    public Task<List<VehicleVisit>> GetActiveAsync() =>
        _db.Table<VehicleVisit>()
           .Where(v => v.ExitAt == null)
           .OrderBy(v => v.ParkingSpace)
           .ToListAsync();

    public Task<List<VehicleVisit>> GetHistoryAsync() =>
        _db.Table<VehicleVisit>()
           .OrderByDescending(v => v.EntryAt)
           .ToListAsync();

    public async Task<long> GetTodayIncomeAsync()
    {
        var start = DateTime.Today;
        var end = start.AddDays(1);
        var visits = await _db.Table<VehicleVisit>()
            .Where(v => v.ExitAt != null && v.ExitAt >= start && v.ExitAt < end)
            .ToListAsync();
        return visits.Sum(v => v.AmountPaid);
    }

    public async Task<(bool Success, string Message)> RegisterEntryAsync(
        string plate, string customerName, int parkingSpace)
    {
        plate = plate.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(plate))
            return (false, "Escribe la placa de la moto.");

        if (parkingSpace < 1 || parkingSpace > Capacity)
            return (false, "Selecciona un puesto válido.");

        var existing = await _db.Table<VehicleVisit>()
            .Where(v => v.Plate == plate && v.ExitAt == null)
            .FirstOrDefaultAsync();
        if (existing is not null)
            return (false, "Esta placa ya tiene una entrada activa.");

        var occupied = await _db.Table<VehicleVisit>()
            .Where(v => v.ParkingSpace == parkingSpace && v.ExitAt == null)
            .FirstOrDefaultAsync();
        if (occupied is not null)
            return (false, "Ese puesto ya está ocupado.");

        var activeCount = await _db.Table<VehicleVisit>()
            .Where(v => v.ExitAt == null)
            .CountAsync();
        if (activeCount >= Capacity)
            return (false, "El parqueadero está lleno.");

        await _db.InsertAsync(new VehicleVisit
        {
            Plate = plate,
            CustomerName = customerName.Trim(),
            ParkingSpace = parkingSpace,
            EntryAt = DateTime.Now
        });

        return (true, $"Entrada registrada para {plate}.");
    }

    public async Task<(bool Success, string Message, long Amount)> RegisterExitAsync(
        string plate, long hourlyRate, string paymentMethod)
    {
        plate = plate.Trim().ToUpperInvariant();
        var visit = await _db.Table<VehicleVisit>()
            .Where(v => v.Plate == plate && v.ExitAt == null)
            .FirstOrDefaultAsync();

        if (visit is null)
            return (false, "No hay una moto activa con esa placa.", 0);

        var exitAt = DateTime.Now;
        var minutes = Math.Max(1, (exitAt - visit.EntryAt).TotalMinutes);
        var hours = Math.Max(1, (long)Math.Ceiling(minutes / 60.0));
        var amount = hours * hourlyRate;

        visit.ExitAt = exitAt;
        visit.AmountPaid = amount;
        visit.PaymentMethod = paymentMethod;
        await _db.UpdateAsync(visit);

        return (true, $"Salida registrada. Tiempo cobrado: {hours} hora(s). Total: {amount:C0}.", amount);
    }

    public Task<int> GetOccupiedCountAsync() =>
        _db.Table<VehicleVisit>().Where(v => v.ExitAt == null).CountAsync();
}
