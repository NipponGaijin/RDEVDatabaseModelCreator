using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RDEVDatabaseModelCreator.RdevType;

namespace RDEVDatabaseModelCreator.Classes
{
    public class RdevField
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public RdevTypes? Type { get; set; }

        public RdevTable RelatedTable { get; set; }

        public List<RdevEnumItem> Enum { get; set; }

    }

    public class RdevEnumItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
