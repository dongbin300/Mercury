namespace Mercury.Charts.Technicals
{
    public enum CheckpointDirection
    {
        Up,
        Down
    }

    public class Checkpoint
    {
        public DateTime Time { get; set; }
        public CheckpointDirection Direction { get; set; }
        public decimal Price { get; set; }

        public Checkpoint(DateTime time, CheckpointDirection direction, decimal price)
        {
            Time = time;
            Direction = direction;
            Price = price;
        }

        public override string ToString()
        {
            return (Direction == CheckpointDirection.Up ? "+" : "-") + Price;
        }
    }
}
