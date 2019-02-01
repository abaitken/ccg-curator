namespace CCGCurator
{
    class ImageFeed
    {
        public ImageFeed(string name, int filterIndex)
        {
            Name = name;
            FilterIndex = filterIndex;
        }

        public string Name { get; }
        public int FilterIndex { get; }
    }
}