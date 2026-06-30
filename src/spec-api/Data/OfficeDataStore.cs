using spec_api.Models;

namespace spec_api.Data;

public static class OfficeDataStore
{
    private static readonly List<Office> _offices = new()
    {
        new Office(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "London HQ",
            "100 Victoria Street, London, UK",
            "+44 20 7946 0958",
            "london@company.com"
        ),
        new Office(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "New York Office",
            "350 5th Ave, New York, NY 10118, USA",
            "+1 212 563 5000",
            "ny@company.com"
        )
    };

    private static readonly object _lock = new();

    public static List<Office> GetAll()
    {
        lock (_lock)
        {
            return [.. _offices];
        }
    }

    public static Office? GetById(Guid id)
    {
        lock (_lock)
        {
            return _offices.FirstOrDefault(o => o.Id == id);
        }
    }

    public static void Add(Office office)
    {
        lock (_lock)
        {
            _offices.Add(office);
        }
    }

    public static bool Update(Office updatedOffice)
    {
        lock (_lock)
        {
            var index = _offices.FindIndex(o => o.Id == updatedOffice.Id);
            if (index == -1) return false;
            _offices[index] = updatedOffice;
            return true;
        }
    }

    public static bool Delete(Guid id)
    {
        lock (_lock)
        {
            var office = _offices.FirstOrDefault(o => o.Id == id);
            if (office == null) return false;
            _offices.Remove(office);
            return true;
        }
    }
}
