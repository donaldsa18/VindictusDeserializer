using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ServiceCore.EndPointNetwork;

namespace PacketCap.Database.Char
{
    class CharacterCostume
    {
        public ObjectId Id;

        public string CharacterID;
        public int BodyPaintingMode;
        public int BodyPaintingClip;
        public int BodyPaintingSide;
        public int BodyPaintingSize;
        public int BodyPaintingRotation;
        public int BodyPaintingPosY;
        public int BodyPaintingPosX;
        public int VIPCode;
        public bool IsReturn;
        public int CafeType;
        public bool HideHeadCostume;
        public int PaintingSize;
        public int PaintingRotation;
        public int PaintingPosY;
        public int PaintingPosX;
        public int Bust;
        public int Height;
        public int Shineness;
        public BsonDocument DecorationColorInfo;
        public BsonDocument DecorationInfo;
        public BsonDocument EffectInfo;
        public BsonDocument PollutionInfo;
        public BsonDocument AvatarHideInfo;
        public BsonDocument AvatarInfo;
        public BsonDocument ColorInfo;
        public BsonDocument CostumeTypeInfo;
        public BsonDocument BodyShapeInfo;
        public int TownEffect;

        public CharacterCostume(CostumeInfo c, string name) {
            this.Id = ObjectId.GenerateNewId();
            this.CharacterID = name;
            this.BodyPaintingMode = c.BodyPaintingMode;
            this.BodyPaintingClip = c.BodyPaintingClip;
            this.BodyPaintingSide = c.BodyPaintingSide;
            this.BodyPaintingSize = c.BodyPaintingSize;
            this.BodyPaintingRotation = c.BodyPaintingRotation;
            this.BodyPaintingPosY = c.BodyPaintingPosY;
            this.BodyPaintingPosX = c.BodyPaintingPosX;
            this.VIPCode = c.VIPCode;
            this.IsReturn = c.IsReturn;
            this.CafeType = c.CafeType;
            this.HideHeadCostume = c.HideHeadCostume;
            this.PaintingSize = c.PaintingSize;
            this.PaintingRotation = c.PaintingRotation;
            this.PaintingPosY = c.PaintingPosY;
            this.PaintingPosX = c.PaintingPosX;
            this.Bust = c.Bust;
            this.Height = c.Height;
            this.Shineness = c.Shineness;
            
            this.DecorationColorInfo = DictToBsonDoc<int,int>(c.DecorationColorInfo);
            this.DecorationInfo = DictToBsonDoc<int, int>(c.DecorationInfo);
            this.EffectInfo = DictToBsonDoc<int, int>(c.EffectInfo);
            this.PollutionInfo = DictToBsonDoc<int, byte>(c.PollutionInfo);
            this.AvatarHideInfo = DictToBsonDoc<int, int>(c.AvatarHideInfo);
            this.AvatarInfo = DictToBsonDoc<int, bool>(c.AvatarInfo);
            this.ColorInfo = DictToBsonDoc<int, int>(c.ColorInfo);
            this.CostumeTypeInfo = DictToBsonDoc<int, int>(c.CostumeTypeInfo);
            this.BodyShapeInfo = DictToBsonDoc<int, float>(c.BodyShapeInfo);
            this.TownEffect = c.TownEffect;
        }
        private BsonDocument DictToBsonDoc<T1,T2>(IDictionary<T1, T2> dict) {
            Dictionary<string,object> strDict = new Dictionary<string, object>(dict.Count);
            foreach (KeyValuePair<T1,T2> entry in dict) {
                if (typeof(T2) == typeof(byte))
                {
                    strDict[entry.Key.ToString()] = int.Parse(entry.Value.ToString());
                }
                else {
                    strDict[entry.Key.ToString()] = entry.Value;
                }
                
            }
            return strDict.ToBsonDocument();
        }

        private string BsonDocumentToString(BsonDocument doc, string name) {
            StringBuilder sb = new StringBuilder();
            sb.Append("\t\t");
            sb.Append(name);
            sb.Append(":");
            foreach(var field in doc.ToDictionary()) {
                sb.Append("\n\t\t\t");
                sb.Append(field.Key);
                sb.Append("=");
                sb.Append(field.Value.ToString());
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\tCharacterCostume:");
            int startLen = sb.Length;
            string t = "\n\t\t";

            sb.Append(t);
            sb.Append("Shineness=");
            sb.Append(Shineness);

            sb.Append(t);
            sb.Append("Height=");
            sb.Append(Height);

            sb.Append(t);
            sb.Append("Bust=");
            sb.Append(Bust);

            sb.Append(t);
            sb.Append("PaintingPosX=");
            sb.Append(PaintingPosX);

            sb.Append(t);
            sb.Append("PaintingPosY=");
            sb.Append(PaintingPosY);

            sb.Append(t);
            sb.Append("PaintingRotation=");
            sb.Append(PaintingRotation);

            sb.Append(t);
            sb.Append("PaintingSize=");
            sb.Append(PaintingSize);

            sb.Append(t);
            sb.Append("HideHeadCostume=");
            sb.Append(HideHeadCostume);

            sb.Append(t);
            sb.Append("CafeType=");
            sb.Append(CafeType);

            sb.Append(t);
            sb.Append("IsReturn=");
            sb.Append(IsReturn);

            sb.Append(t);
            sb.Append("VIPCode=");
            sb.Append(VIPCode);

            String temp = BsonDocumentToString(CostumeTypeInfo, "CostumeTypeInfo");
            sb.Append("\n");
            sb.Append(temp);

            temp = BsonDocumentToString(ColorInfo, "ColorInfo");
            sb.Append("\n");
            sb.Append(temp);
            
            temp = BsonDocumentToString(AvatarInfo, "AvatarInfo");
            sb.Append("\n");
            sb.Append(temp);

            temp = BsonDocumentToString(AvatarHideInfo, "AvatarHideInfo");
            sb.Append("\n");
            sb.Append(temp);

            temp = BsonDocumentToString(PollutionInfo, "PollutionInfo");
            sb.Append("\n");
            sb.Append(temp);

            temp = BsonDocumentToString(EffectInfo, "EffectInfo");
            sb.Append("\n");
            sb.Append(temp);

            temp = BsonDocumentToString(DecorationInfo, "DecorationInfo");
            sb.Append("\n");
            sb.Append(temp);

            temp = BsonDocumentToString(DecorationColorInfo, "DecorationColorInfo");
            sb.Append("\n");
            sb.Append(temp);

            temp = BsonDocumentToString(BodyShapeInfo,"BodyShapeInfo");
            sb.Append("\n");
            sb.Append(temp);

            return sb.ToString();
        }
    }
}
