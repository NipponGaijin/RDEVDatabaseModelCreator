using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDEVDatabaseModelCreator.Classes
{
    public class RdevTable
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public List<RdevField> Fields { get; set; } = new List<RdevField>();

        public void AddField(RdevField field)
        {
            Fields.Add(field);
        }
    }
}
