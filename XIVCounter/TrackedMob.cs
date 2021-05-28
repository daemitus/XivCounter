namespace XIVCounter
{
    internal class TrackedMob
    {
        public int ActorID { get; private set; }

        public string Name { get; private set; }

        public bool Alive { get; set; }

        public TrackedMob(int actorId, string name)
        {
            this.ActorID = actorId;
            this.Name = name;
        }
    }
}
