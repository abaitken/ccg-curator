using System.Drawing;
using CCGCurator.Data;

namespace CCGCurator.ReferenceBuilder
{
    internal abstract class CardImageSource
    {
        internal abstract Bitmap GetImage(Card card, Set set);
    }
}