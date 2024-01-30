namespace GeneticLab.Models
{
    public class City(string name, int x, int y)
    {
        public string Name { get; } = name;
        public int X { get; } = x;
        public int Y { get; } = y;
    }
}
