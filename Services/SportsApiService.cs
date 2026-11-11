
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoyalPlay.Services;

public class SportsEvent
{
    public string Id { get; set; } = "";
    public string Sport { get; set; } = "Soccer";
    public string League { get; set; } = "";

    public string Home { get; set; } = "";
    public string Away { get; set; } = "";

    public DateTime StartUtc { get; set; }

    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }

    public string Status { get; set; } = "";

    public bool IsFinal =>
        Status.Equals(
            "FT",
            StringComparison.OrdinalIgnoreCase)
        ||
        Status.Contains(
            "Finished",
            StringComparison.OrdinalIgnoreCase)
        ||
        Status.Contains(
            "Full Time",
            StringComparison.OrdinalIgnoreCase)
        ||
        Status.Contains(
            "After Penalties",
            StringComparison.OrdinalIgnoreCase)
        ||
        Status.Equals(
            "AET",
            StringComparison.OrdinalIgnoreCase);

    public string Outcome
    {
        get
        {
            if (!IsFinal ||
                !HomeScore.HasValue ||
                !AwayScore.HasValue)
            {
                return "";
            }

            if (HomeScore > AwayScore)
                return "H";

            if (AwayScore > HomeScore)
                return "A";

            return "D";
        }
    }

    public string TimeText =>
        StartUtc.ToLocalTime().ToString(
            "ddd dd MMM · HH:mm",
            CultureInfo.InvariantCulture);
}

public static class SportsApiService
{
    private const string Root =
        "https://www.thesportsdb.com/api/v1/json/123/";

    private static readonly HttpClient client = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly Dictionary<string, int[]> Leagues =
        new()
        {
            // Major football leagues.
            ["Soccer"] = new[]
            {
                4328, // English Premier League
                4335, // Spanish La Liga
                4331, // German Bundesliga
                4332, // Italian Serie A
                4334  // French Ligue 1
            },

            // NBA.
            ["Basketball"] = new[]
            {
                4387
            },

            // ATP and WTA.
            ["Tennis"] = new[]
            {
                4464,
                4517
            }
        };

    // Avoid repeatedly calling the free API.
    private static readonly Dictionary<
        string,
        (DateTime Updated, List<SportsEvent> Events)
    > cache = new();

    private static readonly object cacheLock = new();

    public static async Task<List<SportsEvent>> UpcomingAsync(
        string sport)
    {
        if (sport == "Football")
            sport = "Soccer";

        if (!Leagues.ContainsKey(sport))
            sport = "Soccer";

        lock (cacheLock)
        {
            if (cache.TryGetValue(sport, out var saved))
            {
                if (DateTime.UtcNow - saved.Updated <
                    TimeSpan.FromMinutes(5))
                {
                    return saved.Events
                        .Where(e =>
                            e.StartUtc > DateTime.UtcNow)
                        .ToList();
                }
            }
        }

        var matches = new List<SportsEvent>();

        int successfulRequests = 0;

        foreach (int leagueId in Leagues[sport])
        {
            try
            {
                string url =
                    $"{Root}eventsnextleague.php?id={leagueId}";

                var events = await ReadEventsAsync(url);

                matches.AddRange(
                    events.Where(e => e.Sport == sport));

                successfulRequests++;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"League {leagueId}: {ex.Message}");
            }
        }

        // If league endpoints return no future games,
        // try the limited daily schedule as fallback.
        bool hasUpcoming = matches.Any(e =>
            e.StartUtc > DateTime.UtcNow);

        if (!hasUpcoming)
        {
            for (int day = 0; day < 3; day++)
            {
                try
                {
                    string date = DateTime.UtcNow
                        .Date
                        .AddDays(day)
                        .ToString(
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture);

                    string url =
                        $"{Root}eventsday.php?d={date}" +
                        $"&s={Uri.EscapeDataString(sport)}";

                    var events =
                        await ReadEventsAsync(url);

                    matches.AddRange(
                        events.Where(e => e.Sport == sport));

                    successfulRequests++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Daily fixtures: {ex.Message}");
                }
            }
        }

        if (successfulRequests == 0)
        {
            throw new HttpRequestException(
                "Could not connect to the sports data provider.");
        }

        var upcoming = matches
            .Where(e =>
                e.StartUtc > DateTime.UtcNow &&
                !string.IsNullOrEmpty(e.Id))
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .OrderBy(e => e.StartUtc)
            .Take(24)
            .ToList();

        lock (cacheLock)
        {
            cache[sport] =
                (DateTime.UtcNow, upcoming);
        }

        return upcoming;
    }

    public static async Task<SportsEvent?> LookupAsync(
        string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId) ||
            !eventId.All(char.IsDigit))
        {
            return null;
        }

        string url =
            $"{Root}lookupevent.php?id={eventId}";

        var events = await ReadEventsAsync(url);

        return events.FirstOrDefault();
    }

    private static async Task<List<SportsEvent>> ReadEventsAsync(
        string url)
    {
        using var response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();

        string json =
            await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);

        var result = new List<SportsEvent>();

        if (!document.RootElement.TryGetProperty(
                "events",
                out var array)
            ||
            array.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in array.EnumerateArray())
        {
            string id = Field(item, "idEvent");
            string home = Field(item, "strHomeTeam");
            string away = Field(item, "strAwayTeam");

            // Tennis fixtures sometimes only contain
            // player names inside strEvent.
            if (string.IsNullOrWhiteSpace(home) ||
                string.IsNullOrWhiteSpace(away))
            {
                string title = Field(item, "strEvent");

                int separator = title.IndexOf(
                    " vs ",
                    StringComparison.OrdinalIgnoreCase);

                if (separator >= 0)
                {
                    home = title[..separator].Trim();

                    away = title[
                        (separator + 4)..].Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(home) ||
                string.IsNullOrWhiteSpace(away))
            {
                continue;
            }

            string timestamp =
                Field(item, "strTimestamp");

            bool validDate = DateTime.TryParse(
                timestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out DateTime start);

            if (!validDate)
            {
                string fallback =
                    Field(item, "dateEvent") +
                    " " +
                    Field(item, "strTime");

                validDate = DateTime.TryParse(
                    fallback,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal |
                    DateTimeStyles.AdjustToUniversal,
                    out start);
            }

            if (!validDate)
                continue;

            result.Add(new SportsEvent
            {
                Id = id,
                Sport = Field(item, "strSport"),
                League = Field(item, "strLeague"),
                Home = home,
                Away = away,
                StartUtc = start,
                Status = Field(item, "strStatus"),

                HomeScore =
                    IntField(item, "intHomeScore"),

                AwayScore =
                    IntField(item, "intAwayScore")
            });
        }

        return result;
    }

    private static string Field(
        JsonElement element,
        string name)
    {
        if (element.TryGetProperty(
                name,
                out var value)
            &&
            value.ValueKind != JsonValueKind.Null)
        {
            return value.ToString() ?? "";
        }

        return "";
    }

    private static int? IntField(
        JsonElement element,
        string name)
    {
        string value = Field(element, name);

        return int.TryParse(
            value,
            out int number)
            ? number
            : null;
    }
}
