
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace RoyalPlay.Services;

public record PlayingCard(int Value, string Suit)
{
    public string Rank => Value switch
    {
        1 => "A",
        11 => "J",
        12 => "Q",
        13 => "K",
        _ => Value.ToString()
    };

    public string Symbol => Suit;
}

public class CardDeck
{
    private readonly List<PlayingCard> cards = new();

    public CardDeck()
    {
        foreach (string suit in new[] { "♠", "♥", "♦", "♣" })
        {
            for (int value = 1; value <= 13; value++)
            {
                cards.Add(new PlayingCard(value, suit));
            }
        }

        // Fisher-Yates shuffle.
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);

            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }

    public PlayingCard Draw()
    {
        if (cards.Count == 0)
            throw new InvalidOperationException("Deck is empty.");

        PlayingCard card = cards[^1];
        cards.RemoveAt(cards.Count - 1);

        return card;
    }
}

public class BlackjackGame
{
    private readonly CardDeck deck = new();

    public List<PlayingCard> Player { get; } = new();
    public List<PlayingCard> Dealer { get; } = new();

    public int Bet { get; private set; }
    public int Profit { get; private set; }

    public bool Finished { get; private set; }
    public string Result { get; private set; } = "";

    public bool CanDouble =>
        !Finished && Player.Count == 2;

    public BlackjackGame(int bet)
    {
        Bet = bet;

        Player.Add(deck.Draw());
        Dealer.Add(deck.Draw());

        Player.Add(deck.Draw());
        Dealer.Add(deck.Draw());

        if (Value(Player) == 21 || Value(Dealer) == 21)
            Finish();
    }

    public static int Value(IEnumerable<PlayingCard> cards)
    {
        int sum = 0;
        int aces = 0;

        foreach (var card in cards)
        {
            if (card.Value == 1)
            {
                sum += 11;
                aces++;
            }
            else
            {
                sum += Math.Min(card.Value, 10);
            }
        }

        while (sum > 21 && aces > 0)
        {
            sum -= 10;
            aces--;
        }

        return sum;
    }

    public void Hit()
    {
        if (Finished)
            return;

        Player.Add(deck.Draw());

        if (Value(Player) >= 21)
            Stand();
    }

    public void Double()
    {
        if (!CanDouble)
            return;

        Bet *= 2;
        Player.Add(deck.Draw());
        Stand();
    }

    public void Stand()
    {
        if (Finished)
            return;

        if (Value(Player) <= 21)
        {
            while (Value(Dealer) < 17)
            {
                Dealer.Add(deck.Draw());
            }
        }

        Finish();
    }

    private void Finish()
    {
        Finished = true;

        int playerScore = Value(Player);
        int dealerScore = Value(Dealer);

        bool playerNatural =
            Player.Count == 2 && playerScore == 21;

        bool dealerNatural =
            Dealer.Count == 2 && dealerScore == 21;

        if (playerNatural && !dealerNatural)
        {
            Result = "BLACKJACK!";
            Profit = Bet * 3 / 2;
        }
        else if (dealerNatural && !playerNatural)
        {
            Result = "DEALER BLACKJACK";
            Profit = -Bet;
        }
        else if (playerScore > 21)
        {
            Result = "BUST — YOU LOSE";
            Profit = -Bet;
        }
        else if (dealerScore > 21 || playerScore > dealerScore)
        {
            Result = "YOU WIN!";
            Profit = Bet;
        }
        else if (playerScore == dealerScore)
        {
            Result = "PUSH — TIE";
            Profit = 0;
        }
        else
        {
            Result = "DEALER WINS";
            Profit = -Bet;
        }
    }
}
