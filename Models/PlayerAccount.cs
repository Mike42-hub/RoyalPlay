
using SQLite;

namespace RoyalPlay.Models;

public class PlayerAccount
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string UsernameKey { get; set; } = "";

    public string Username { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string PasswordSalt { get; set; } = "";

    public int Chips { get; set; } = 10000;

    public int XP { get; set; } = 0;

    public int GamesPlayed { get; set; } = 0;
}
