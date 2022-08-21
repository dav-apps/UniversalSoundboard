using HtmlAgilityPack;
using System;
using System.Collections.Generic;
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

        public override async Task<SoundDownloadResult> GetResult()
        {
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(Url);

            // Get the tracklist
            var tracklistNode = document.DocumentNode.SelectNodes("//table[@id='tracklist']/*");

            if (tracklistNode == null)
                throw new SoundDownloadException();

            List<SoundDownloadListItem> soundItems = new List<SoundDownloadListItem>();

            foreach (var node in tracklistNode)
            {
                // Get the name
                var nameNode = node.SelectSingleNode("./td[@class='name']");
                if (nameNode == null) continue;

                string name = nameNode.InnerText;

                // Get the download link
                var downloadNode = node.SelectSingleNode("./td[@class='download']/a");
                if (downloadNode == null) continue;

                string downloadLink = downloadNode.GetAttributeValue("href", null);
                if (downloadLink == null) continue;

                soundItems.Add(new SoundDownloadListItem(name, downloadLink));
            }

            // Get the header
            var headerNode = document.DocumentNode.SelectSingleNode("//div[@id='music_info']/h2");
            string categoryName = null;

            if (headerNode != null)
                categoryName = headerNode.InnerText;

            // Get the cover
            var coverNode = document.DocumentNode.SelectSingleNode("//div[@id='music_cover']/img");
            Uri imgSourceUri = null;

            if (coverNode != null)
            {
                string imgSource = coverNode.GetAttributeValue("src", null);

                if (imgSource != null)
                    imgSourceUri = new Uri(imgSource);
            }

            return new SoundDownloadResult(null, imgSourceUri, categoryName, soundItems);
        }
    }
}
