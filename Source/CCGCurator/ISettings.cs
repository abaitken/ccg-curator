using System.Linq;
using CCGCurator.Data;

namespace CCGCurator
{
    interface ISettings
    {
        int WebcamIndex { get; set; }
        int RotationDegrees { get; set; }
        void Save();
    }

    static class SettingsHelper
    {
        public static int ValidateRotation(int currentValue)
        {
            var validValues = EnumerableExtensions.Range(-180, 180, 90);
            if (validValues.Contains(currentValue))
                return currentValue;
            return 0;
        }


        public static int ValidateWebCamIndex(int currentValue, int currentFeedCount)
        {
            if (currentValue >= currentFeedCount || currentValue < 0)
                return 0;

            return currentValue;
        }
    }
}

namespace CCGCurator.Properties
{
    partial class Settings : ISettings
    {
        
    }
}