using System;
using System.Collections.Generic;
using System.Linq;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class SetCategoriesDialog : Dialog
    {
        private WinUI.TreeView CategoriesTreeView;
        public IList<object> SelectedItems
        {
            get => CategoriesTreeView?.SelectedItems;
        }

        public SetCategoriesDialog(List<Sound> sounds)
            : base(
                  GetTitle(sounds),
                  "",
                  ""
            )
        {
            Content = GetContent(sounds);
        }

        private static string GetTitle(List<Sound> sounds)
        {
            if (sounds.Count == 1)
                return string.Format(FileManager.loader.GetString("SetCategoriesDialog-Title"), sounds[0].Name);
            else
                return string.Format(FileManager.loader.GetString("SetCategoriesDialog-MultipleSounds-Title"), sounds.Count);
        }

        private StackPanel GetContent(List<Sound> sounds)
        {
            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            CategoriesTreeView = new WinUI.TreeView
            {
                Height = 300,
                SelectionMode = WinUI.TreeViewSelectionMode.Multiple,
                CanDrag = false,
                CanDragItems = false,
                CanReorderItems = false,
                AllowDrop = false
            };

            // Get all categories
            List<Category> categories = new List<Category>();
            for (int i = 1; i < FileManager.itemViewHolder.Categories.Count; i++)
                categories.Add(FileManager.itemViewHolder.Categories[i]);

            // Find the intersection of the categories of all sounds
            List<Guid> soundCategories = new List<Guid>();
            foreach (var category in sounds.First().Categories)
                if (sounds.TrueForAll(s => s.Categories.Exists(c => c.Uuid == category.Uuid)))
                    soundCategories.Add(category.Uuid);

            // Create the nodes and add them to the tree view
            List<CustomTreeViewNode> selectedNodes = new List<CustomTreeViewNode>();
            foreach (var node in FileManager.CreateTreeViewNodesFromCategories(categories, selectedNodes, soundCategories))
                CategoriesTreeView.RootNodes.Add(node);

            foreach (var node in selectedNodes)
                CategoriesTreeView.SelectedNodes.Add(node);

            if (categories.Count > 0)
            {
                content.Children.Add(CategoriesTreeView);

                ContentDialog.PrimaryButtonText = FileManager.loader.GetString("Actions-Save");
                ContentDialog.CloseButtonText = FileManager.loader.GetString("Actions-Cancel");
            }
            else
            {
                TextBlock noCategoriesTextBlock = new TextBlock
                {
                    Text = FileManager.loader.GetString("SetCategoriesDialog-NoCategories")
                };
                content.Children.Add(noCategoriesTextBlock);

                ContentDialog.CloseButtonText = FileManager.loader.GetString("Actions-Close");
            }

            return content;
        }
    }
}
