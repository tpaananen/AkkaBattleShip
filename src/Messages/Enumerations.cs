namespace Messages.CSharp
{
    public enum GameStatus
    {
        None = 0,
        Created = 1,
        PlayerJoined = 2,
        GameStartedYouStart = 3,
        GameStartedOpponentStarts = 4,
        ItIsYourTurn = 5,
        YouWon = 6,
        YouLost = 7,
        GameOver = 8,
        Configured = 9
    }
}
