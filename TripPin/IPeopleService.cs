using TripPin.Model;

namespace TripPin;

public interface IPeopleService
{
    Task<List<User>> Search(string? filter);

    Task<User> GetByUserName(string userName);

    Task<bool> UpdateUserField(string userName, Dictionary<string, string> fieldsToUpdate);
}