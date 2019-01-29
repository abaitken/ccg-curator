using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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