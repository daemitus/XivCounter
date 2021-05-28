namespace XIVCounter
{
    internal class Counter
    {
        public string Name { get; set; }

        public bool Automated { get; set; }

        public int Count { get; set; }

        public Counter(string name) => this.Name = name;
    }
}
