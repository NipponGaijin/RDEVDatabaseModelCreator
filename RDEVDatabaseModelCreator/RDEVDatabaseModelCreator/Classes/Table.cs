using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDEVDatabaseModelCreator.Classes
{
    public class Table
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public List<Field> Fields { get; set; }

        public void AddField(Field field)
        {
            Fields.Add(field);
        }
    }
}
