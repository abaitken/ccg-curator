namespace CCGCurator
{
    interface ISettings
    {
        int WebcamIndex { get; set; }
        int RotationDegrees { get; set; }
        void Save();
    }
}

namespace CCGCurator.Properties
{
    partial class Settings : ISettings
    {
    }
}