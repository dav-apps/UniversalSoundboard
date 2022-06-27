using Microsoft.Toolkit.Uwp.UI.Controls;
using UniversalSoundboard.Common;
using Windows.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Models
{
    public class InAppNotificationItem
    {
        public InAppNotification InAppNotification { get; }
        public InAppNotificationType InAppNotificationType { get; }
        public int Duration { get; set; }
        public WinUI.ProgressRing ProgressRing { get; set; }
        public TextBlock MessageTextBlock { get; }
        public Button PrimaryButton { get; }
        public Button SecondaryButton { get; }
        public bool Sent { get; set; }

        public InAppNotificationItem(
            InAppNotification inAppNotification,
            InAppNotificationType inAppNotificationType,
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
