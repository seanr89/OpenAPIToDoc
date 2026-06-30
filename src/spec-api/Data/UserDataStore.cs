using spec_api.Models;

namespace spec_api.Data;

public static class UserDataStore
{
    private static readonly List<User> _users = new()
    {
        new User(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Alice",
            "Smith",
            "alice.smith@company.com",
            "Manager",
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        ),
        new User(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Bob",
            "Jones",
            "bob.jones@company.com",
            "Developer",
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        ),
        new User(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "Charlie",
            "Brown",
            "charlie.brown@company.com",
            "Designer",
            Guid.Parse("22222222-2222-2222-2222-222222222222")
        )
    };

    private static readonly object _lock = new();

    public static List<User> GetAll()
    {
        lock (_lock)
        {
            return [.. _users];
        }
    }

    public static User? GetById(Guid id)
    {
        lock (_lock)
        {
            return _users.FirstOrDefault(u => u.Id == id);
        }
    }

    public static void Add(User user)
    {
        lock (_lock)
        {
            _users.Add(user);
        }
    }

    public static bool Update(User updatedUser)
    {
        lock (_lock)
        {
            var index = _users.FindIndex(u => u.Id == updatedUser.Id);
            if (index == -1) return false;
            _users[index] = updatedUser;
            return true;
        }
    }

    public static bool Delete(Guid id)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return false;
            _users.Remove(user);
            return true;
        }
    }
}
