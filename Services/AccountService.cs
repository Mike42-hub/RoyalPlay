
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Maui.Storage;
using RoyalPlay.Models;
using SQLite;

namespace RoyalPlay.Services;

public static class AccountService
{
    private const int HashIterations = 150000;

    private static SQLiteAsyncConnection? database;

    private static readonly SemaphoreSlim databaseLock =
        new SemaphoreSlim(1, 1);

    public static PlayerAccount? CurrentUser { get; private set; }

    // Opens the database and creates the table if needed.
    private static async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (database != null)
            return database;

        await databaseLock.WaitAsync();

        try
        {
            if (database == null)
            {
                string path = Path.Combine(
                    FileSystem.AppDataDirectory,
                    "RoyalPlay.db3");

                var connection = new SQLiteAsyncConnection(path);

                await connection.CreateTableAsync<PlayerAccount>();

                database = connection;
            }

            return database!;
        }
        finally
        {
            databaseLock.Release();
        }
    }

    // Returns null on success, or an error message.
    public static async Task<string?> RegisterAsync(
        string username,
        string password)
    {
        username = username.Trim();

        if (username.Length < 3 || username.Length > 20)
        {
            return "Username must be 3 to 20 characters.";
        }

        foreach (char c in username)
        {
            bool valid =
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') ||
                c == '_';

            if (!valid)
            {
                return "Use letters, numbers or underscore only.";
            }
        }

        if (password.Length < 8)
        {
            return "Password must be at least 8 characters.";
        }

        string usernameKey = username.ToLowerInvariant();

        var db = await GetDatabaseAsync();

        var existingUser = await db.Table<PlayerAccount>()
            .Where(u => u.UsernameKey == usernameKey)
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            return "This username is already taken.";
        }

        // Generate a random salt for this account.
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        // Hash the password. Never store the original password.
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            HashIterations,
            HashAlgorithmName.SHA256,
            32);

        var newUser = new PlayerAccount
        {
            Username = username,
            UsernameKey = usernameKey,
            PasswordSalt = Convert.ToBase64String(salt),
            PasswordHash = Convert.ToBase64String(hash),
            Chips = 10000,
            XP = 0,
            GamesPlayed = 0
        };

        await db.InsertAsync(newUser);

        CurrentUser = newUser;

        return null;
    }

    // Checks the username and password.
    public static async Task<string?> LoginAsync(
        string username,
        string password)
    {
        username = username.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password))
        {
            return "Enter your username and password.";
        }

        var db = await GetDatabaseAsync();

        var user = await db.Table<PlayerAccount>()
            .Where(u => u.UsernameKey == username)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return "Incorrect username or password.";
        }

        byte[] salt = Convert.FromBase64String(user.PasswordSalt);

        byte[] savedHash = Convert.FromBase64String(user.PasswordHash);

        byte[] enteredHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            HashIterations,
            HashAlgorithmName.SHA256,
            32);

        bool correctPassword =
            CryptographicOperations.FixedTimeEquals(
                savedHash,
                enteredHash);

        if (!correctPassword)
        {
            return "Incorrect username or password.";
        }

        CurrentUser = user;

        return null;
    }

    // Clears the current login session.
    public static void Logout()
    {
        CurrentUser = null;
    }
}
