using System.Linq;
using CCGCurator.Data;
using CCGCurator.Data.Extensions;

namespace CCGCurator
{
    interface ISettings
    {
        int WebcamIndex { get; set; }
        int RotationDegrees { get; set; }
        bool ZoomToDetectedCard { get; set; }
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