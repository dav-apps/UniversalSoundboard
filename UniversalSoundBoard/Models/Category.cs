namespace UniversalSoundBoard.Models
{
    public class Category
    {
        public string Uuid { get; }
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

        public Category(string uuid, string name, string icon)
        {
            Uuid = uuid;
            Name = name;
            Icon = icon;
        }
    }
}
