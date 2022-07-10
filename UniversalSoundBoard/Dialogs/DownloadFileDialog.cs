using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class DownloadFileDialog : Dialog
    {
        public ProgressBar ProgressBar { get; private set; }

        public DownloadFileDialog(string filename)
            : base(
                  string.Format(FileManager.loader.GetString("DownloadFileDialog-Title"), filename),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            Content = GetContent();
        }

        private StackPanel GetContent()
        {
            StackPanel content = new StackPanel
            {
                Margin = new Thickness(0, 30, 0, 0),
                Orientation = Orientation.Vertical
            };

            var downloadFileProgressBar = new ProgressBar
            {
                IsIndeterminate = true
            };

            content.Children.Add(downloadFileProgressBar);
            return content;
        }
    }
}
