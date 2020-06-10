using System;
using System.Collections.Generic;

namespace UniversalSoundBoard.Models
{
    public class Category
    {
        public Guid Uuid { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public List<Category> Children { get; set; }

        public Category() { }

        public Category(string name, string icon)
        {
            Name = name;
            Icon = icon;
            Children = new List<Category>();
        }

        public Category(Guid uuid, string name, string icon)
        {
            Uuid = uuid;
            Name = name;
            Icon = icon;
            Children = new List<Category>();
        }

        public Category(Guid uuid, string name, string icon, List<Category> children)
        {
            Uuid = uuid;
            Name = name;
            Icon = icon;
            Children = children;
        }
    }
}
