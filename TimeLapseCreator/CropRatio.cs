namespace TimeLapseCreator
{
    public class CropRatio
    {
        public string Name { get; }
        public int Horizontal { get; }
        public int Vertical { get; }

        public CropRatio(string name, int horizontal, int vertical)
        {
            Name = name;
            Horizontal = horizontal;
            Vertical = vertical;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
