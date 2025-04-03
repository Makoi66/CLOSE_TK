class Program
{
    static void Main(string[] args)
    {
        using (Game game = new Game(1600, 800))
        {
            game.Run();
        }
    }
}