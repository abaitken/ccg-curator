using System.Drawing;
using CCGCurator.Data.Model;

namespace CCGCurator.ReferenceBuilder.ImageSources
{
    internal abstract class CardImageSource
    {
        internal abstract Bitmap GetImage(Card card, Set set);
    }
}