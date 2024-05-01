namespace TripPin.Model;

public class User
{
    public string UserName { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string MiddleName { get; set; }

    public string Gender { get; set; }

    public List<string> Emails { get; set; }

    public string FavoriteFeature { get; set; }

    public List<string> Features { get; set; }

    public List<AddressInfo> AddressInfo { get; set; }

    public AddressInfo HomeAddress { get; set; }
}