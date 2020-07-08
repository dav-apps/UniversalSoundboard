using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public class CustomTreeViewNode : WinUI.TreeViewNode
    {
        public object Tag { get; set; }

        public CustomTreeViewNode() : base() { }
    }
}
