using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalSoundBoard.Model
{
    public class Category
    {
        public string Name { get; set; }
        public string Icon { get; set; }

        public static implicit operator string(Category v)
        {
            throw new NotImplementedException();
        }
    }
}
