using UniversalSoundBoard.Models;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class CategoryOrderItemTemplate : UserControl
    {
        public Category Category { get { return DataContext as Category; } }

        public CategoryOrderItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
        }
    }
}
