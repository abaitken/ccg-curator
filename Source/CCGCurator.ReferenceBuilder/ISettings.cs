namespace CCGCurator.ReferenceBuilder
{
    interface ISettings
    {
        string ImageCachePath { get; set; }
        void Save();
    }
}