using MongoDB.Bson.Serialization.Attributes;
using ServiceCore.EndPointNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketCap.Database
{
    [BsonIgnoreExtraElements]
    class Location
    {
        public long CID;
        public float X;
        public float Y;
        public float Vx;
        public float Vy;
        public float Yaw;
        public DateTime Time;
        public long Channel;
        public int TownID;

        public Location(NotifyAction a,long channel,int townID) {
            this.CID = a.ID;
            this.X = a.Action.Position.X;
            this.Y = a.Action.Position.Y;
            this.Vx = a.Action.Velocity.X;
            this.Vy = a.Action.Velocity.Y;
            this.Yaw = a.Action.Yaw;
            this.Time = DateTime.Now;
            this.Channel = channel;
            this.TownID = townID;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Location:\n\tCID=");
            sb.Append(CID);
            sb.Append("\n\tX=");
            sb.Append(X);
            sb.Append("\n\tY=");
            sb.Append(Y);
            sb.Append("\n\tVx=");
            sb.Append(Vx);
            sb.Append("\n\tVy=");
            sb.Append(Vy);
            sb.Append("\n\tYaw=");
            sb.Append(Yaw);
            sb.Append("\n\tTime=");
            sb.Append(MessagePrinter.DateTimeToString(Time));
            sb.Append("\n\tChannel=");
            sb.Append(Channel);
            return sb.ToString();
        }
    }
}
