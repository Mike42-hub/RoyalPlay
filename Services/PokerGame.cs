
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace RoyalPlay.Services;

public sealed class PokerOpponent
{
    public string Name { get; init; } = "";
    public string Style { get; init; } = "";
    public double Aggression { get; init; }
    public double BluffChance { get; init; }

    public int Stack { get; set; } = 1500;
    public int StreetPaid { get; set; }
    public bool Active { get; set; } = true;
    public string LastMove { get; set; } = "WAITING";

    public List<PlayingCard> Cards { get; } = new();
}

// Fixed-limit Texas Hold'em.
// One bet and a maximum of one raise per betting round.
// No all-in or side pots.
public sealed class PokerGame
{
    private const int Ante = 20;

    private readonly CardDeck deck = new();
    private readonly int startingBalance;

    private readonly Dictionary<string, double>
        equityCache = new();

    private int myStreetPaid;
    private bool raiseUsed;

    public List<PlayingCard> MyCards { get; } = new();
    public List<PlayingCard> Community { get; } = new();
    public List<PokerOpponent> Opponents { get; } = new();
    public List<string> RecentActions { get; } = new();

    public int Pot { get; private set; }
    public int PlayerSpent { get; private set; }
    public int Round { get; private set; }
    public int ToCall { get; private set; }
    public int Profit { get; private set; }

    public bool Finished { get; private set; }
    public bool ShowAllCards { get; private set; }

    public string Message { get; private set; } =
        "YOUR TURN";

    public int BetSize => Round <= 1 ? 40 : 80;

    public string RoundName =>
        new[] { "PRE-FLOP", "FLOP", "TURN", "RIVER" }
        [Math.Min(Round, 3)];

    public string MyHandName =>
        Community.Count < 3
            ? "HOLE CARDS"
            : PokerHandEvaluator.CategoryName(
                MyCards.Concat(Community).ToList());

    public bool CanCall =>
        !Finished &&
        PlayerSpent + ToCall <= startingBalance;

    public bool CanRaise =>
        !Finished &&
        !raiseUsed &&
        PlayerSpent + ToCall + BetSize <= startingBalance;

    public PokerGame(int balance, int opponentCount)
    {
        if (balance < 60)
            throw new ArgumentException(
                "You need at least 60 chips.");

        startingBalance = balance;

        opponentCount = Math.Clamp(opponentCount, 1, 3);

        var profiles = new[]
        {
            ("BLAZE", "Aggressive", 0.85, 0.16),
            ("SAGE", "Careful", 0.34, 0.025),
            ("ACE", "Balanced", 0.60, 0.085)
        };

        for (int i = 0; i < opponentCount; i++)
        {
            var p = profiles[i];

            Opponents.Add(new PokerOpponent
            {
                Name = p.Item1,
                Style = p.Item2,
                Aggression = p.Item3,
                BluffChance = p.Item4
            });
        }

        // Deal two cards to each player.
        for (int card = 0; card < 2; card++)
        {
            MyCards.Add(deck.Draw());

            foreach (var bot in Opponents)
                bot.Cards.Add(deck.Draw());
        }

        PlayerSpent = Ante;

        foreach (var bot in Opponents)
            bot.Stack -= Ante;

        Pot = Ante * (Opponents.Count + 1);

        BeginStreet();
    }

    public void Fold()
    {
        if (Finished)
            return;

        RecentActions.Clear();

        Finished = true;
        Profit = -PlayerSpent;

        Message =
            $"YOU FOLDED  •  -{PlayerSpent} CHIPS";

        RecentActions.Add(Message);
    }

    public void CheckOrCall()
    {
        if (!CanCall)
            return;

        RecentActions.Clear();

        if (ToCall > 0)
        {
            int amount = ToCall;

            PayPlayer(amount);

            RecentActions.Add($"YOU CALL {amount}");

            ToCall = 0;
        }
        else
        {
            RecentActions.Add("YOU CHECK");
        }

        NextStreet();
    }

    public void Raise()
    {
        if (!CanRaise)
            return;

        RecentActions.Clear();

        bool respondingToBet = ToCall > 0;

        int amount = ToCall + BetSize;

        PayPlayer(amount);
        ToCall = 0;

        RecentActions.Add(
            respondingToBet
                ? $"YOU RAISE TO {myStreetPaid}"
                : $"YOU BET {BetSize}");

        if (respondingToBet)
            raiseUsed = true;

        // Computer opponents decide how to respond.
        foreach (var bot in Opponents
            .Where(x => x.Active)
            .ToList())
        {
            int required =
                myStreetPaid - bot.StreetPaid;

            if (required <= 0)
                continue;

            if (bot.Stack >= required &&
                ShouldCall(bot, required))
            {
                PayBot(bot, required);

                bot.LastMove = $"CALL {required}";

                RecentActions.Add(
                    $"{bot.Name} CALLS {required}");
            }
            else
            {
                FoldBot(bot);
            }
        }

        if (Opponents.All(x => !x.Active))
        {
            WinByFolds();
            return;
        }

        // One computer may re-raise an opening bet.
        if (!respondingToBet)
        {
            var reraiser = Opponents
                .Where(x =>
                    x.Active &&
                    x.StreetPaid == myStreetPaid &&
                    x.Stack >= BetSize)
                .FirstOrDefault(ShouldReRaise);

            if (reraiser != null)
            {
                PayBot(reraiser, BetSize);

                reraiser.LastMove = $"RAISE {BetSize}";

                RecentActions.Add(
                    $"{reraiser.Name} RAISES {BetSize}!");

                raiseUsed = true;

                foreach (var other in Opponents
                    .Where(x =>
                        x.Active &&
                        x != reraiser)
                    .ToList())
                {
                    int required =
                        reraiser.StreetPaid -
                        other.StreetPaid;

                    if (other.Stack >= required &&
                        ShouldCall(other, required))
                    {
                        PayBot(other, required);

                        other.LastMove =
                            $"CALL {required}";

                        RecentActions.Add(
                            $"{other.Name} CALLS {required}");
                    }
                    else
                    {
                        FoldBot(other);
                    }
                }

                ToCall =
                    reraiser.StreetPaid - myStreetPaid;

                Message =
                    $"{reraiser.Name} RE-RAISED. " +
                    $"CALL {ToCall} OR FOLD.";

                return;
            }
        }

        NextStreet();
    }

    private void BeginStreet()
    {
        myStreetPaid = 0;
        raiseUsed = false;
        ToCall = 0;

        equityCache.Clear();

        foreach (var bot in Opponents
            .Where(x => x.Active))
        {
            bot.StreetPaid = 0;
            bot.LastMove = "CHECK";
        }

        var opener = Opponents
            .Where(x =>
                x.Active &&
                x.Stack >= BetSize)
            .FirstOrDefault(ShouldOpenBet);

        if (opener == null)
        {
            Message =
                $"{RoundName}: OPPONENTS CHECK. " +
                "YOUR MOVE.";

            foreach (var bot in Opponents
                .Where(x => x.Active))
            {
                RecentActions.Add(
                    $"{bot.Name} CHECKS");
            }

            return;
        }

        PayBot(opener, BetSize);

        opener.LastMove = $"BET {BetSize}";

        RecentActions.Add(
            $"{opener.Name} BETS {BetSize}");

        foreach (var bot in Opponents
            .Where(x => x.Active && x != opener)
            .ToList())
        {
            if (bot.Stack >= BetSize &&
                ShouldCall(bot, BetSize))
            {
                PayBot(bot, BetSize);

                bot.LastMove = $"CALL {BetSize}";

                RecentActions.Add(
                    $"{bot.Name} CALLS {BetSize}");
            }
            else
            {
                FoldBot(bot);
            }
        }

        ToCall = BetSize;

        Message =
            $"{opener.Name} BET {BetSize}. " +
            "CALL, RAISE, OR FOLD.";
    }

    private void NextStreet()
    {
        if (Opponents.All(x => !x.Active))
        {
            WinByFolds();
            return;
        }

        Round++;

        if (Round == 1)
        {
            deck.Draw(); // Burn card.

            for (int i = 0; i < 3; i++)
                Community.Add(deck.Draw());
        }
        else if (Round is 2 or 3)
        {
            deck.Draw();

            Community.Add(deck.Draw());
        }
        else
        {
            Showdown();
            return;
        }

        RecentActions.Add($"--- {RoundName} ---");

        BeginStreet();
    }

    private void Showdown()
    {
        Finished = true;
        ShowAllCards = true;

        var scores = new List<(string Name, long Score)>
        {
            (
                "YOU",
                PokerHandEvaluator.BestScore(
                    MyCards.Concat(Community).ToList())
            )
        };

        foreach (var bot in Opponents
            .Where(x => x.Active))
        {
            scores.Add((
                bot.Name,
                PokerHandEvaluator.BestScore(
                    bot.Cards.Concat(Community).ToList())
            ));
        }

        long top = scores.Max(x => x.Score);

        var winners = scores
            .Where(x => x.Score == top)
            .Select(x => x.Name)
            .ToList();

        int share = winners.Contains("YOU")
            ? Pot / winners.Count
            : 0;

        Profit = share - PlayerSpent;

        Message =
            $"SHOWDOWN: {string.Join(" + ", winners)} WIN. " +
            $"YOU: {Profit:+#;-#;0} CHIPS";

        RecentActions.Add(Message);
    }

    private void WinByFolds()
    {
        Finished = true;

        Profit = Pot - PlayerSpent;

        Message =
            $"ALL OPPONENTS FOLDED. " +
            $"YOU WIN {Profit} CHIPS!";

        RecentActions.Add(Message);
    }

    private void FoldBot(PokerOpponent bot)
    {
        bot.Active = false;
        bot.LastMove = "FOLD";

        RecentActions.Add($"{bot.Name} FOLDS");
    }

    private void PayPlayer(int amount)
    {
        PlayerSpent += amount;
        myStreetPaid += amount;
        Pot += amount;
    }

    private void PayBot(
        PokerOpponent bot,
        int amount)
    {
        bot.Stack -= amount;
        bot.StreetPaid += amount;
        Pot += amount;
    }

    // Computer decision-making.

    private bool ShouldOpenBet(PokerOpponent bot)
    {
        double equity = EstimateEquity(bot);

        double threshold =
            0.70 - bot.Aggression * 0.15;

        return equity >= threshold
            ? Chance(
                0.44 + bot.Aggression * 0.44)
            : Chance(bot.BluffChance);
    }

    private bool ShouldCall(
        PokerOpponent bot,
        int cost)
    {
        double equity = EstimateEquity(bot);

        double potOdds =
            (double)cost / (Pot + cost);

        double margin =
            equity +
            bot.Aggression * 0.09 -
            potOdds;

        double probability = margin switch
        {
            > 0.25 => 0.96,
            > 0.12 => 0.86,
            > 0.04 => 0.65,
            > -0.07 => 0.30,
            _ => bot.BluffChance * 0.6
        };

        return Chance(probability);
    }

    private bool ShouldReRaise(PokerOpponent bot)
    {
        return EstimateEquity(bot) > 0.77 &&
            Chance(
                0.08 + 0.24 * bot.Aggression);
    }

    // Monte Carlo simulation:
    // Generate 48 possible random deals.
    // Compare the computer's hand against
    // a simulated opponent.
    // The bot never sees the human's actual cards.
    private double EstimateEquity(PokerOpponent bot)
    {
        if (equityCache.TryGetValue(
            bot.Name,
            out double cached))
        {
            return cached;
        }

        var excluded = bot.Cards
            .Concat(Community)
            .ToHashSet();

        var available = new List<PlayingCard>();

        foreach (string suit in new[]
        {
            "♠", "♥", "♦", "♣"
        })
        {
            for (int value = 1; value <= 13; value++)
            {
                var card = new PlayingCard(value, suit);

                if (!excluded.Contains(card))
                    available.Add(card);
            }
        }

        double wins = 0;
        const int trials = 48;

        for (int t = 0; t < trials; t++)
        {
            var pool =
                new List<PlayingCard>(available);

            PlayingCard Take()
            {
                int index =
                    RandomNumberGenerator.GetInt32(
                        pool.Count);

                var picked = pool[index];

                pool.RemoveAt(index);

                return picked;
            }

            var rival = new List<PlayingCard>
            {
                Take(), Take()
            };

            var board =
                new List<PlayingCard>(Community);

            while (board.Count < 5)
                board.Add(Take());

            long mine =
                PokerHandEvaluator.BestScore(
                    bot.Cards.Concat(board).ToList());

            long theirs =
                PokerHandEvaluator.BestScore(
                    rival.Concat(board).ToList());

            wins += mine > theirs
                ? 1.0
                : mine == theirs
                    ? 0.5
                    : 0.0;
        }

        double equity = wins / trials;

        equityCache[bot.Name] = equity;

        return equity;
    }

    private static bool Chance(double probability)
    {
        return RandomNumberGenerator.GetInt32(10000)
            < Math.Clamp(probability, 0, 1) * 10000;
    }
}

// Evaluates 5, 6 or 7 cards by checking every
// possible combination of five cards.
public static class PokerHandEvaluator
{
    private static readonly string[] Names =
    {
        "HIGH CARD",
        "ONE PAIR",
        "TWO PAIR",
        "THREE OF A KIND",
        "STRAIGHT",
        "FLUSH",
        "FULL HOUSE",
        "FOUR OF A KIND",
        "STRAIGHT FLUSH"
    };

    public static string CategoryName(
        List<PlayingCard> cards)
    {
        long score = BestScore(cards);

        int category = (int)(score / 759375L);

        return Names[
            Math.Clamp(category, 0, Names.Length - 1)];
    }

    public static long BestScore(
        List<PlayingCard> cards)
    {
        if (cards.Count < 5)
        {
            throw new ArgumentException(
                "At least 5 cards required.");
        }

        long best = 0;

        for (int a = 0; a < cards.Count - 4; a++)
            for (int b = a + 1; b < cards.Count - 3; b++)
                for (int c = b + 1; c < cards.Count - 2; c++)
                    for (int d = c + 1; d < cards.Count - 1; d++)
                        for (int e = d + 1; e < cards.Count; e++)
                        {
                            best = Math.Max(
                                best,
                                ScoreFive(new[]
                                {
                    cards[a],
                    cards[b],
                    cards[c],
                    cards[d],
                    cards[e]
                                }));
                        }

        return best;
    }

    private static long ScoreFive(
        PlayingCard[] cards)
    {
        int[] ranks = cards
            .Select(c =>
                c.Value == 1 ? 14 : c.Value)
            .OrderByDescending(x => x)
            .ToArray();

        bool flush = cards.All(
            c => c.Suit == cards[0].Suit);

        int[] unique = ranks.Distinct().ToArray();

        int straight = 0;

        if (unique.Length == 5)
        {
            if (unique[0] - unique[4] == 4)
            {
                straight = unique[0];
            }
            else if (unique.SequenceEqual(
                new[] { 14, 5, 4, 3, 2 }))
            {
                straight = 5;
            }
        }

        var groups = ranks
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key)
            .ToList();

        int[] ordered = groups
            .SelectMany(g =>
                Enumerable.Repeat(
                    g.Key,
                    g.Count()))
            .ToArray();

        if (flush && straight > 0)
            return Encode(8, straight);

        if (groups[0].Count() == 4)
            return Encode(
                7,
                groups[0].Key,
                groups[1].Key);

        if (groups[0].Count() == 3 &&
            groups[1].Count() == 2)
        {
            return Encode(
                6,
                groups[0].Key,
                groups[1].Key);
        }

        if (flush)
            return Encode(5, ranks);

        if (straight > 0)
            return Encode(4, straight);

        if (groups[0].Count() == 3)
            return Encode(3, ordered);

        if (groups[0].Count() == 2 &&
            groups[1].Count() == 2)
            return Encode(2, ordered);

        if (groups[0].Count() == 2)
            return Encode(1, ordered);

        return Encode(0, ranks);
    }

    private static long Encode(
        int category,
        params int[] kickers)
    {
        long value = category;

        for (int i = 0; i < 5; i++)
        {
            value = value * 15 +
                (i < kickers.Length ? kickers[i] : 0);
        }

        return value;
    }
}
