using HtmlAgilityPack;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadMyInstantsPlugin : SoundDownloadPlugin
    {
        public SoundDownloadMyInstantsPlugin(string url) : base(url) { }

        public override bool IsUrlMatch()
        {
            /** 
             * Example: https://www.myinstants.com/en/instant/zu-nebenrisiken-und-wirkungen-94456/
             */
            Regex urlRegex = new Regex("^(https?:\\/\\/)?(www.)?myinstants.com\\/\\w+\\/instant\\/[\\w\\-]+\\/?");
            return urlRegex.IsMatch(Url);
        }

        public override async Task<SoundDownloadPluginResult> GetResult()
        {
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(Url);

            // Get the name
            var nameNode = document.DocumentNode.SelectSingleNode("//h1[@id='instant-page-title']");
            if (nameNode == null) return null;

            string name = nameNode.InnerText;

            // Get the download link
            var downloadNode = document.DocumentNode.SelectSingleNode("//div[@id='instant-page-extra-buttons-container']/a[@download]");
            if (downloadNode == null) return null;

            string downloadUrl = downloadNode.GetAttributeValue("href", null);
            if (downloadUrl == null) return null;

            downloadUrl = "https://www.myinstants.com" + downloadUrl;

            // Try to get the audio file metadata
            WebResponse response;

            try
            {
                var req = WebRequest.Create(downloadUrl);
                response = await req.GetResponseAsync();

                // Check if the content type is a supported audio format
                if (!Constants.allowedAudioMimeTypes.Contains(response.ContentType))
                {
                    Analytics.TrackEvent("AudioFileDownload-NotSupportedFormat", new Dictionary<string, string>
                    {
                        { "Link", Url }
                    });

                    throw new SoundDownloadException();
                }
            }
            catch (Exception e)
            {
                Crashes.TrackError(e, new Dictionary<string, string>
                {
                    { "Link", Url }
                });

                throw new SoundDownloadException();
            }

            // Get file type and file size
            string audioFileType = FileManager.FileTypeToExt(response.ContentType);
            long fileSize = response.ContentLength;

            return new SoundDownloadPluginResult(new List<SoundDownloadItem>
            {
                new SoundDownloadItem(name, Url, null, downloadUrl, null, audioFileType, 0, fileSize)
            });
        }
    }
}
