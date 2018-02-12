namespace UniversalSoundBoard.Models
{
    public class Category
    {
        public string Name { get; set; }
        public string Icon { get; set; }

        public Category()
        {
        }

        public Category(string name, string icon)
        {
            Name = name;
            Icon = icon;
        }
    }
}
