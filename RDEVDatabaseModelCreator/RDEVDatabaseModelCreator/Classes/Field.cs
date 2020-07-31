using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDEVDatabaseModelCreator.Classes
{
    public class Field
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public RdevTypes Type { get; set; }
    }
}
