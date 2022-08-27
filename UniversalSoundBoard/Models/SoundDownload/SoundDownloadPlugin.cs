using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadPlugin
    {
        public string Url { get; }
        
        public SoundDownloadPlugin(string url)
        {
            Url = url;
        }

        public virtual bool IsUrlMatch()
        {
            // Regex for generic url
            Regex urlRegex = new Regex("^(https?:\\/\\/)?[\\w.-]+(\\.[\\w.-]+)+[\\w\\-._~/?#@&%\\+,;=]+");
            return urlRegex.IsMatch(Url);
        }

        public virtual async Task<SoundDownloadPluginResult> GetResult()
        {
            // Make a GET request to see if this is an audio file
            WebResponse response;

            try
            {
                var req = WebRequest.Create(Url);
                response = await req.GetResponseAsync();

                // Check if the content type is a supported audio format
                if (!FileManager.allowedAudioMimeTypes.Contains(response.ContentType))
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

            // Try to get the file name
            Regex fileNameRegex = new Regex("^.+\\.\\w{3}$");
            string audioFileName = null;
            string lastPart = HttpUtility.UrlDecode(Url.Split('/').Last());

            if (fileNameRegex.IsMatch(lastPart))
            {
                var parts = lastPart.Split('.');
                audioFileName = string.Join(".", parts.Take(parts.Count() - 1));
            }

            return new SoundDownloadPluginResult(new List<SoundDownloadItem>
            {
                new SoundDownloadItem(audioFileName, null, Url, null, audioFileType, 0, fileSize)
            });
        }
    }
}
