using System.Drawing;
using System.Net;
using CCGCurator.Data.Model;

namespace CCGCurator.ReferenceBuilder.ImageSources
{
    internal class WebImageSource : CardImageSource
    {
        internal override Bitmap GetImage(Card card, Set set)
        {
            var requestUriString =
                $"http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid={card.MultiverseId}&type=card";
            var request =
                WebRequest.Create(
                    requestUriString);
            var resp = request.GetResponse();
            var responseStream = resp.GetResponseStream();
            var bitmap = new Bitmap(responseStream);
            responseStream.Dispose();
            return bitmap;
        }
    }
}