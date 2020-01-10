using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketCap.Database
{
    [BsonIgnoreExtraElements]
    class Translation
    {
        public string name;
        public string value;
        public Translation(string name, string value) {
            this.name = name;
            this.value = value;
        }
    }
}
