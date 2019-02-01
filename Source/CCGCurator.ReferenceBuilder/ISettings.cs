namespace CCGCurator.ReferenceBuilder
{
    interface ISettings
    {
        string ImageCachePath { get; set; }
        void Save();
    }
}

namespace CCGCurator.ReferenceBuilder.Properties
{
    partial class Settings : ISettings
    {
    }
}