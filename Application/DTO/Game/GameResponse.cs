namespace Application.DTO.Game
{
    public class GameResponse
    {
        public int GameId { get; set; }
        public required string Name { get; set; }
        public double Price { get; set; }

    }
}