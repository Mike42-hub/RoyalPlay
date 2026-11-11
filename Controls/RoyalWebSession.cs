
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using RoyalPlay.Pages;
using RoyalPlay.Services;

namespace RoyalPlay.Controls;

public sealed class RoyalWebSession
{
    private const string BridgePrefix =
        "https://royalplay.local/bridge?";

    private readonly string startScreen;
    private readonly WebView web;
    private readonly SemaphoreSlim gate = new(1, 1);

    private readonly Dictionary<string, SportsEvent>
        loadedEvents = new();

    private BlackjackGame? blackjack;
    private PokerGame? poker;

    private bool blackjackSaved;
    private bool pokerSaved;
    private bool sourceAssigned;
    private bool pageReady;
    private bool navigateAway;

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase
        };

    public View View => web;

    public RoyalWebSession(string initialScreen)
    {
        startScreen = initialScreen;

        web = new WebView
        {
            BackgroundColor =
                Color.FromArgb("#071118"),

            HorizontalOptions =
                LayoutOptions.Fill,

            VerticalOptions =
                LayoutOptions.Fill
        };

        web.Navigating += OnNavigating;
        web.Navigated += OnNavigated;
    }

    public async Task LoadAsync()
    {
        if (sourceAssigned)
        {
            await PushStateAsync();
            return;
        }

        string html =
            await ReadAssetAsync("royalplay.html");

        string cssA =
            await ReadAssetAsync("royal-a.css");

        string cssB =
            await ReadAssetAsync("royal-b.css");

        string js =
            await ReadAssetAsync("royal-ui.js");

        html = html
            .Replace("/*CSS_A*/", cssA)
            .Replace("/*CSS_B*/", cssB)
            .Replace("/*APP_JS*/", js);

        sourceAssigned = true;

        web.Source = new HtmlWebViewSource
        {
            Html = html,
            BaseUrl = "https://royalplay.local/"
        };
    }

    private static async Task<string> ReadAssetAsync(
        string filename)
    {
        using var file =
            await FileSystem.OpenAppPackageFileAsync(
                filename);

        using var reader = new StreamReader(file);

        return await reader.ReadToEndAsync();
    }

    public async Task OpenSectionAsync(string screen)
    {
        if (!pageReady)
            return;

        string value = JsonSerializer.Serialize(screen);

        await JavaScriptAsync(
            $"Royal.go({value});");
    }

    private async void OnNavigated(
        object? sender,
        WebNavigatedEventArgs e)
    {
        if (e.Result != WebNavigationResult.Success ||
            navigateAway)
        {
            return;
        }

        pageReady = true;

        try
        {
            await PushStateAsync();

            if (startScreen == "login" ||
                startScreen == "register")
            {
                string value =
                    JsonSerializer.Serialize(startScreen);

                await JavaScriptAsync(
                    $"Royal.setAuthMode({value});");
            }
            else
            {
                string value =
                    JsonSerializer.Serialize(startScreen);

                await JavaScriptAsync(
                    $"Royal.go({value});");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private async void OnNavigating(
        object? sender,
        WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith(
            BridgePrefix,
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        e.Cancel = true;

        await gate.WaitAsync();

        try
        {
            var query = ParseQuery(
                new Uri(e.Url).Query);

            string command = Arg(query, "cmd");

            await HandleCommandAsync(command, query);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);

            await ErrorAsync(
                "That action could not be completed.");
        }
        finally
        {
            gate.Release();
        }
    }

    private static Dictionary<string, string> ParseQuery(
        string query)
    {
        var items = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var part in query
            .TrimStart('?')
            .Split(
                '&',
                StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);

            if (pair.Length == 0)
                continue;

            string key = Uri.UnescapeDataString(
                pair[0].Replace('+', ' '));

            string value = pair.Length == 2
                ? Uri.UnescapeDataString(
                    pair[1].Replace('+', ' '))
                : "";

            items[key] = value;
        }

        return items;
    }

    private static string Arg(
        IReadOnlyDictionary<string, string> args,
        string key)
    {
        return args.TryGetValue(
            key,
            out string? value)
            ? value
            : "";
    }

    private async Task HandleCommandAsync(
        string command,
        IReadOnlyDictionary<string, string> args)
    {
        var user = AccountService.CurrentUser;

        switch (command)
        {
            case "route.register":
                {
                    navigateAway = true;

                    await Shell.Current.GoToAsync(
                        nameof(RegisterPage));

                    return;
                }

            case "route.login":
                {
                    navigateAway = true;

                    await Shell.Current.GoToAsync("..");

                    return;
                }

            case "auth.login":
            case "auth.register":
                {
                    string name = Arg(args, "username");
                    string password = Arg(args, "password");

                    string? error =
                        command == "auth.login"
                        ? await AccountService.LoginAsync(
                            name, password)
                        : await AccountService.RegisterAsync(
                            name, password);

                    if (error != null)
                    {
                        await ErrorAsync(error);
                        return;
                    }

                    navigateAway = true;

                    await Shell.Current.GoToAsync(
                        "//main/home");

                    return;
                }

            case "auth.logout":
                {
                    if (poker is { Finished: false })
                    {
                        poker.Fold();

                        await SavePokerAsync();
                    }

                    if (blackjack is { Finished: false } &&
                        !blackjackSaved)
                    {
                        blackjackSaved = true;

                        await GameDataService.RecordGameAsync(
                            "Blackjack",
                            "Left table",
                            -blackjack.Bet);
                    }

                    AccountService.Logout();

                    navigateAway = true;

                    await Shell.Current.GoToAsync("//login");

                    return;
                }

            case "settings.motion":
                {
                    Preferences.Default.Set(
                        "RoyalPlayAnimations",
                        Arg(args, "value") == "1");

                    break;
                }

            case "poker.start":
                {
                    if (user == null)
                    {
                        await ErrorAsync(
                            "Please log in first.");

                        return;
                    }

                    if (poker is { Finished: false })
                    {
                        await ErrorAsync(
                            "Finish this hand first.");

                        return;
                    }

                    if (!int.TryParse(
                        Arg(args, "bots"),
                        out int bots))
                    {
                        bots = 1;
                    }

                    if (user.Chips < 60)
                    {
                        await ErrorAsync(
                            "At least 60 chips are needed.");

                        return;
                    }

                    poker = new PokerGame(
                        user.Chips,
                        Math.Clamp(bots, 1, 3));

                    pokerSaved = false;

                    await SavePokerAsync();

                    break;
                }

            case "poker.call":
                {
                    if (poker is not { Finished: false } ||
                        !poker.CanCall)
                    {
                        return;
                    }

                    poker.CheckOrCall();

                    await SavePokerAsync();

                    break;
                }

            case "poker.raise":
                {
                    if (poker is not { Finished: false } ||
                        !poker.CanRaise)
                    {
                        return;
                    }

                    poker.Raise();

                    await SavePokerAsync();

                    break;
                }

            case "poker.fold":
                {
                    if (poker is not { Finished: false })
                        return;

                    poker.Fold();

                    await SavePokerAsync();

                    break;
                }

            case "blackjack.start":
                {
                    if (user == null)
                    {
                        await ErrorAsync(
                            "Please log in first.");

                        return;
                    }

                    if (blackjack is { Finished: false })
                    {
                        await ErrorAsync(
                            "Finish the current round first.");

                        return;
                    }

                    if (!int.TryParse(
                        Arg(args, "bet"),
                        out int bet)
                        ||
                        !new[]
                        {
                        50, 100, 250, 500, 1000
                        }.Contains(bet))
                    {
                        await ErrorAsync(
                            "Invalid Blackjack stake.");

                        return;
                    }

                    if (user.Chips < bet)
                    {
                        await ErrorAsync(
                            "Not enough chips.");

                        return;
                    }

                    blackjack = new BlackjackGame(bet);

                    blackjackSaved = false;

                    await SaveBlackjackAsync();

                    break;
                }

            case "blackjack.hit":
                {
                    if (blackjack is not { Finished: false })
                        return;

                    blackjack.Hit();

                    await SaveBlackjackAsync();

                    break;
                }

            case "blackjack.stand":
                {
                    if (blackjack is not { Finished: false })
                        return;

                    blackjack.Stand();

                    await SaveBlackjackAsync();

                    break;
                }

            case "blackjack.double":
                {
                    if (blackjack is not { Finished: false } ||
                        !blackjack.CanDouble ||
                        user == null ||
                        user.Chips < blackjack.Bet * 2)
                    {
                        return;
                    }

                    blackjack.Double();

                    await SaveBlackjackAsync();

                    break;
                }

            case "sports.load":
                {
                    string sport = Arg(args, "sport");

                    if (sport is not
                        ("Soccer" or "Basketball" or "Tennis"))
                    {
                        sport = "Soccer";
                    }

                    var fixtures =
                        await SportsApiService.UpcomingAsync(
                            sport);

                    foreach (var match in fixtures)
                    {
                        loadedEvents[match.Id] = match;
                    }

                    await PushMatchesAsync(fixtures);
                    await PushPredictionsAsync();

                    break;
                }

            case "sports.predictions":
                {
                    await PushPredictionsAsync();
                    break;
                }

            case "sports.pick":
                {
                    string id = Arg(args, "id");
                    string choice = Arg(args, "choice");

                    if (!loadedEvents.TryGetValue(
                        id,
                        out var match))
                    {
                        await ErrorAsync(
                            "Match expired. Refresh fixtures.");

                        return;
                    }

                    string? error =
                        await GameDataService.SavePredictionAsync(
                            match,
                            choice);

                    if (error != null)
                    {
                        await ErrorAsync(error);
                        return;
                    }

                    await ToastAsync(
                        "Prediction saved for " +
                        match.Home +
                        " vs " +
                        match.Away);

                    await PushPredictionsAsync();

                    break;
                }

            case "sports.check":
                {
                    var predictions =
                        await GameDataService.GetPredictionsAsync();

                    int updated = 0;

                    foreach (var prediction in predictions
                        .Where(x =>
                            x.Status == "Pending" &&
                            x.StartUtc <= DateTime.UtcNow)
                        .OrderBy(x => x.StartUtc)
                        .Take(8))
                    {
                        var match =
                            await SportsApiService.LookupAsync(
                                prediction.EventId);

                        if (match != null &&
                            await GameDataService
                                .SettlePredictionAsync(match))
                        {
                            updated++;
                        }
                    }

                    await PushPredictionsAsync();

                    await ToastAsync(
                        $"Result check completed. {updated} updated.");

                    break;
                }

            case "history.load":
                {
                    await PushHistoryAsync();
                    break;
                }

            case "profile.load":
                {
                    break;
                }

            default:
                {
                    await ErrorAsync("Unknown action.");
                    return;
                }
        }

        await PushStateAsync();
    }

    private async Task SavePokerAsync()
    {
        if (poker is not { Finished: true } ||
            pokerSaved)
        {
            return;
        }

        pokerSaved = true;

        try
        {
            await GameDataService.RecordGameAsync(
                "Poker",
                poker.Message,
                poker.Profit);
        }
        catch
        {
            pokerSaved = false;
            throw;
        }
    }

    private async Task SaveBlackjackAsync()
    {
        if (blackjack is not { Finished: true } ||
            blackjackSaved)
        {
            return;
        }

        blackjackSaved = true;

        try
        {
            await GameDataService.RecordGameAsync(
                "Blackjack",
                blackjack.Result,
                blackjack.Profit);
        }
        catch
        {
            blackjackSaved = false;
            throw;
        }
    }

    private static object Card(PlayingCard c)
    {
        return new
        {
            rank = c.Rank,
            suit = c.Suit
        };
    }

    private async Task PushStateAsync()
    {
        if (!pageReady || navigateAway)
            return;

        var user = AccountService.CurrentUser;

        object? pokerDto = poker == null
            ? null
            : new
            {
                active = true,
                finished = poker.Finished,
                pot = poker.Pot,
                profit = poker.Profit,
                roundName = poker.RoundName,

                myHandName =
                    poker.Community.Count >= 3
                    ? PokerHandEvaluator.CategoryName(
                        poker.MyCards
                            .Concat(poker.Community)
                            .ToList())
                    : "HOLE CARDS",

                message = poker.Message,
                toCall = poker.ToCall,
                betSize = poker.BetSize,
                canCall = poker.CanCall,
                canRaise = poker.CanRaise,
                showAllCards = poker.ShowAllCards,

                myCards = poker.MyCards
                    .Select(Card)
                    .ToList(),

                community = poker.Community
                    .Select(Card)
                    .ToList(),

                opponents = poker.Opponents
                    .Select(b => new
                    {
                        name = b.Name,
                        active = b.Active,
                        lastMove = b.LastMove,
                        stack = b.Stack,
                        cards = b.Cards
                            .Select(Card)
                            .ToList()
                    })
                    .ToList(),

                recentActions =
                    poker.RecentActions.ToList()
            };

        object? blackjackDto = blackjack == null
            ? null
            : new
            {
                active = true,
                finished = blackjack.Finished,
                bet = blackjack.Bet,

                result = blackjack.Finished
                    ? $"{blackjack.Result} " +
                      $"({blackjack.Profit:+#;-#;0} chips)"
                    : "",

                playerValue =
                    BlackjackGame.Value(blackjack.Player),

                dealerValue = blackjack.Finished
                    ? BlackjackGame.Value(blackjack.Dealer)
                    : 0,

                canDouble =
                    blackjack.CanDouble &&
                    (user?.Chips ?? 0) >=
                    blackjack.Bet * 2,

                dealer = blackjack.Dealer
                    .Select(Card)
                    .ToList(),

                player = blackjack.Player
                    .Select(Card)
                    .ToList()
            };

        await SendAsync(
            "Royal.setState",
            new
            {
                username = user?.Username ?? "PLAYER",
                chips = user?.Chips ?? 0,
                xp = user?.XP ?? 0,
                gamesPlayed = user?.GamesPlayed ?? 0,

                animations = Preferences.Default.Get(
                    "RoyalPlayAnimations",
                    true),

                poker = pokerDto,
                blackjack = blackjackDto
            });
    }

    private async Task PushMatchesAsync(
        List<SportsEvent> events)
    {
        var result = events
            .Select(x => new
            {
                id = x.Id,
                sport = x.Sport,
                league = x.League,
                home = x.Home,
                away = x.Away,
                timeText = x.TimeText
            })
            .ToList();

        await SendAsync("Royal.setMatches", result);
    }

    private async Task PushPredictionsAsync()
    {
        if (AccountService.CurrentUser == null)
            return;

        var saved =
            await GameDataService.GetPredictionsAsync();

        var result = saved
            .OrderByDescending(x => x.StartUtc)
            .Take(20)
            .Select(x => new
            {
                matchName = x.MatchName,
                choice = x.Choice,
                sport = x.Sport,
                status = x.Status,
                points = x.Points
            })
            .ToList();

        await SendAsync(
            "Royal.setPredictions",
            result);
    }

    private async Task PushHistoryAsync()
    {
        if (AccountService.CurrentUser == null)
            return;

        var records =
            await GameDataService.GetHistoryAsync();

        var result = records
            .Take(60)
            .Select(x => new
            {
                game = x.Game,
                details = x.Details,
                change = x.ChipChange,

                time = x.DateUtc
                    .ToLocalTime()
                    .ToString("dd MMM HH:mm")
            })
            .ToList();

        await SendAsync(
            "Royal.setHistory",
            result);
    }

    private Task ErrorAsync(string message)
    {
        return SendAsync("Royal.error", message);
    }

    private Task ToastAsync(string message)
    {
        return SendAsync("Royal.toast", message);
    }

    private async Task SendAsync(
        string functionName,
        object data)
    {
        if (!pageReady || navigateAway)
            return;

        string json = JsonSerializer.Serialize(
            data,
            JsonOptions);

        await JavaScriptAsync(
            $"{functionName}({json});");
    }

    private async Task JavaScriptAsync(string script)
    {
        if (!pageReady || navigateAway)
            return;

        await web.EvaluateJavaScriptAsync(script);
    }
}
