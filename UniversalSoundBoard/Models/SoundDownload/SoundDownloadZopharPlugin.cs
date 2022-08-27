using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniversalSoundboard.Common;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadZopharPlugin : SoundDownloadPlugin
    {
        public SoundDownloadZopharPlugin(string url) : base(url) { }

        public override bool IsUrlMatch()
        {
            Regex urlRegex = new Regex("^(https?:\\/\\/)?(www.)?zophar.net\\/music\\/[\\w\\-]+\\/[\\w\\-]+");
            return urlRegex.IsMatch(Url);
        }

        public override async Task<SoundDownloadPluginResult> GetResult()
        {
            Regex fileNameRegex = new Regex("^.+\\.\\w{3}$");
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(Url);

            // Get the header
            var headerNode = document.DocumentNode.SelectSingleNode("//div[@id='music_info']/h2");
            string categoryName = null;

            if (headerNode != null)
                categoryName = headerNode.InnerText;

            // Get the cover
            var coverNode = document.DocumentNode.SelectSingleNode("//div[@id='music_cover']/img");
            string imageFileUrl = null;
            string imageFileExt = "jpg";

            if (coverNode != null)
                imageFileUrl = coverNode.GetAttributeValue("src", null);

            if (imageFileUrl != null && fileNameRegex.IsMatch(imageFileUrl))
                imageFileExt = imageFileUrl.Split(".").Last();

            // Get the tracklist
            var tracklistNode = document.DocumentNode.SelectNodes("//table[@id='tracklist']/*");

            if (tracklistNode == null)
                throw new SoundDownloadException();

            List<SoundDownloadItem> soundItems = new List<SoundDownloadItem>();

            foreach (var node in tracklistNode)
            {
                // Get the name
                var nameNode = node.SelectSingleNode("./td[@class='name']");
                if (nameNode == null) continue;

                string name = nameNode.InnerText;

                // Get the download link
                var downloadNode = node.SelectSingleNode("./td[@class='download']/a");
                if (downloadNode == null) continue;

                string audioFileUrl = downloadNode.GetAttributeValue("href", null);
                if (audioFileUrl == null) continue;

                // Get the file ext
                string audioFileExt = "mp3";

                if (fileNameRegex.IsMatch(audioFileUrl))
                    audioFileExt = audioFileUrl.Split(".").Last();

                soundItems.Add(
                    new SoundDownloadItem(
                        name,
                        imageFileUrl,
                        audioFileUrl,
                        imageFileExt,
                        audioFileExt,
                        0,
                        0,
                        true
                    )
                );
            }

            return new SoundDownloadZopharPluginResult(categoryName, imageFileUrl, soundItems);
        }
    }
}
