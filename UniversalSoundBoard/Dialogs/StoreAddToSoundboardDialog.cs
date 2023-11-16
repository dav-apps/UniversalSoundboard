using System;
using System.Collections.Generic;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class StoreAddToSoundboardDialog : Dialog
    {
        private WinUI.TreeView CategoriesTreeView;
        public IList<object> SelectedItems
        {
            get => CategoriesTreeView?.SelectedItems;
        }

        public StoreAddToSoundboardDialog()
            : base("Add to soundboard", "Add", "Cancel", ContentDialogButton.Primary)
        {
            Content = GetContent();
        }

        private StackPanel GetContent()
        {
            StackPanel contentPanel = new StackPanel();

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

            // Create the nodes and add them to the tree view
            List<CustomTreeViewNode> selectedNodes = new List<CustomTreeViewNode>();

            foreach (var node in FileManager.CreateTreeViewNodesFromCategories(categories, selectedNodes, new List<Guid>()))
                CategoriesTreeView.RootNodes.Add(node);

            foreach (var node in selectedNodes)
                CategoriesTreeView.SelectedNodes.Add(node);

            contentPanel.Children.Add(CategoriesTreeView);

            return contentPanel;
        }
    }
}
