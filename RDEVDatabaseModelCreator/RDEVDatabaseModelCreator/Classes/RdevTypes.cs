﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDEVDatabaseModelCreator
{
    public abstract class RdevType
    {
        public enum RdevTypes
        {
            SysString,
            SysInt,
            SysRelation,
            SysDate,
            SysTimeDate,
            SysFile,
            SysBoolean,
            SysGUID,
            SysENUM,
            SysNumber
        }

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
                default:
                    return null;
            }
        }
    }

    
}
