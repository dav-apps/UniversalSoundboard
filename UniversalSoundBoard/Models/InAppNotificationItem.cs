using Microsoft.Toolkit.Uwp.UI.Controls;
using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Models
{
    public class InAppNotificationItem
    {
        public InAppNotification InAppNotification { get; }
        public FileManager.InAppNotificationType InAppNotificationType { get; }
        public int Duration { get; set; }
        public WinUI.ProgressRing ProgressRing { get; set; }
        public TextBlock MessageTextBlock { get; }
        public Button PrimaryButton { get; }
        public Button SecondaryButton { get; }
        public bool Sent { get; set; }

        public InAppNotificationItem(
            InAppNotification inAppNotification,
            FileManager.InAppNotificationType inAppNotificationType,
            int duration,
            WinUI.ProgressRing progressRing,
            TextBlock messageTextBlock,
            Button primaryButton,
            Button secondaryButton
        ) {
            InAppNotification = inAppNotification;
            InAppNotificationType = inAppNotificationType;
            Duration = duration;
            ProgressRing = progressRing;
            MessageTextBlock = messageTextBlock;
            PrimaryButton = primaryButton;
            SecondaryButton = secondaryButton;
            Sent = false;
        }
    }
}
