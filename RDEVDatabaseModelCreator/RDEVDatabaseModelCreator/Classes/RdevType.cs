
using RDEVDatabaseModelCreator.Classes;

namespace RDEVDatabaseModelCreator
{
    public abstract class RdevType
    {
        public static RdevTypes? GetType(string rdevType)
        {
            switch (rdevType)
            {
                case "SysString":
                    return RdevTypes.SysString;
                case "SysInt":
                    return RdevTypes.SysInt;
                case "SysRelation":
                    return RdevTypes.SysRelation;
                case "SysDate":
                    return RdevTypes.SysDate;
                case "SysTimeDate":
                    return RdevTypes.SysTimeDate;
                case "SysFile":
                    return RdevTypes.SysFile;
                case "SysBoolean":
                    return RdevTypes.SysBoolean;
                case "SysGUID":
                    return RdevTypes.SysGUID;
                case "SysENUM":
                    return RdevTypes.SysENUM;
                case "SysNumber":
                    return RdevTypes.SysNumber;
                case "SysDecimal":
                    return RdevTypes.SysDecimal;
                default:
                    return null;
            }
        }
    }

    
}
