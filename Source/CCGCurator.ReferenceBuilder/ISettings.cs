using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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