using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalSoundBoard.Model
{
    public class Data
    {
        public List<Category> Categories { get; set; }
    }

    public class Setting
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public string Id { get; set; }
    }
}
