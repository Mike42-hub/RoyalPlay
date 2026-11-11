
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using RoyalPlay.Models;
using SQLite;

namespace RoyalPlay.Services;

public class GameRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UserId { get; set; }

    public string Game { get; set; } = "";
    public string Details { get; set; } = "";
    public int ChipChange { get; set; }
    public DateTime DateUtc { get; set; }
}

public class SportsPrediction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UserId { get; set; }

    [Indexed]
    public string EventId { get; set; } = "";

    public string Sport { get; set; } = "";
    public string MatchName { get; set; } = "";
    public string Choice { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime StartUtc { get; set; }
    public int Points { get; set; }
}

public static class GameDataService
{
    private static SQLiteAsyncConnection? db;

    private static async Task<SQLiteAsyncConnection> OpenAsync()
    {
        if (db != null)
            return db;

        string path = Path.Combine(
            FileSystem.AppDataDirectory,
            "RoyalPlay.db3");

        var connection = new SQLiteAsyncConnection(path);

        await connection.CreateTableAsync<GameRecord>();
        await connection.CreateTableAsync<SportsPrediction>();

        db = connection;

        return connection;
    }

    public static async Task RecordGameAsync(
        string game,
        string details,
        int chipChange)
    {
        var user = AccountService.CurrentUser;

        if (user == null)
            return;

        var database = await OpenAsync();

        user.Chips = Math.Max(0, user.Chips + chipChange);
        user.GamesPlayed++;

        await database.UpdateAsync(user);

        await database.InsertAsync(new GameRecord
        {
            UserId = user.Id,
            Game = game,
            Details = details,
            ChipChange = chipChange,
            DateUtc = DateTime.UtcNow
        });
    }

    public static async Task<List<GameRecord>> GetHistoryAsync()
    {
        var user = AccountService.CurrentUser;

        if (user == null)
            return new List<GameRecord>();

        var database = await OpenAsync();

        var records = await database.Table<GameRecord>()
            .Where(x => x.UserId == user.Id)
            .ToListAsync();

        return records
            .OrderByDescending(x => x.DateUtc)
            .ToList();
    }

    public static async Task<List<SportsPrediction>> GetPredictionsAsync()
    {
        var user = AccountService.CurrentUser;

        if (user == null)
            return new List<SportsPrediction>();

        var database = await OpenAsync();

        return await database.Table<SportsPrediction>()
            .Where(x => x.UserId == user.Id)
            .ToListAsync();
    }

    public static async Task<string?> SavePredictionAsync(
        SportsEvent game,
        string choice)
    {
        var user = AccountService.CurrentUser;

        if (user == null)
            return "Please log in.";

        if (game.StartUtc <= DateTime.UtcNow)
            return "Predictions are closed for this event.";

        bool valid =
            choice == "H" ||
            choice == "A" ||
            (choice == "D" && game.Sport == "Soccer");

        if (!valid)
            return "Invalid choice.";

        var database = await OpenAsync();

        var old = await database.Table<SportsPrediction>()
            .Where(x =>
                x.UserId == user.Id &&
                x.EventId == game.Id)
            .FirstOrDefaultAsync();

        if (old != null)
            return "You already predicted this match.";

        await database.InsertAsync(new SportsPrediction
        {
            UserId = user.Id,
            EventId = game.Id,
            Sport = game.Sport,
            MatchName = $"{game.Home} vs {game.Away}",
            StartUtc = game.StartUtc,
            Choice = choice
        });

        return null;
    }

    public static async Task<bool> SettlePredictionAsync(
        SportsEvent game)
    {
        if (!game.IsFinal || game.Outcome == "")
            return false;

        var user = AccountService.CurrentUser;

        if (user == null)
            return false;

        var database = await OpenAsync();

        var prediction =
            await database.Table<SportsPrediction>()
            .Where(x =>
                x.UserId == user.Id &&
                x.EventId == game.Id &&
                x.Status == "Pending")
            .FirstOrDefaultAsync();

        if (prediction == null)
            return false;

        bool correct = prediction.Choice == game.Outcome;

        prediction.Status =
            correct ? "Correct" : "Incorrect";

        prediction.Points = correct ? 100 : 0;

        await database.UpdateAsync(prediction);

        if (correct)
        {
            user.XP += prediction.Points;
            await database.UpdateAsync(user);
        }

        return true;
    }
}
