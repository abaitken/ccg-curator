using System;
using System.Drawing;
using System.Net;
using CCGCurator.Data.Model;

namespace CCGCurator.ReferenceBuilder.ImageSources
{
    internal class WebImageSource : CardImageSource
    {
        internal override Bitmap GetImage(Card card, Set set)
        {
            return GetImage($"http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid={card.MultiverseId}&type=card");
        }

        private static Bitmap GetImage(string uri)
        {
            try
            {

                var request =
                    WebRequest.Create(
                        uri);
                var resp = request.GetResponse();
                var responseStream = resp.GetResponseStream();

                if (responseStream == null)
                    return null;
                try
                {
                    var bitmap = new Bitmap(responseStream);
                    return bitmap;
                }
                catch (ArgumentException)
                {
                    return null;
                }
                finally
                {
                    responseStream.Dispose();
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        internal override Bitmap GetImage(Set set, Bitmap missing)
        {
            var result = GetImage($"http://gatherer.wizards.com/Handlers/Image.ashx?type=symbol&set={set.Code}&size=large&rarity=C");

            if (result == null)
                return new Bitmap(missing);
            return result;
        }
    }
}