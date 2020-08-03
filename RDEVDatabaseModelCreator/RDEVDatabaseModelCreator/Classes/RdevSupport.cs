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

public static class RdevTypesInfo
{
    public static System.Collections.Generic.Dictionary<RdevTypes, string> rdevTypesInfo = new System.Collections.Generic.Dictionary<RdevTypes, string>
    {
        { RdevTypes.SysString, "RdevTypes.SysString"},
        { RdevTypes.SysInt, "RdevTypes.SysInt"},
        { RdevTypes.SysRelation, "RdevTypes.SysRelation"},
        { RdevTypes.SysDate, "RdevTypes.SysDate"},
        { RdevTypes.SysTimeDate, "RdevTypes.SysTimeDate"},
        { RdevTypes.SysFile, "RdevTypes.SysFile"},
        { RdevTypes.SysBoolean, "RdevTypes.SysBoolean"},
        { RdevTypes.SysGUID, "RdevTypes.SysGUID"},
        { RdevTypes.SysENUM, "RdevTypes.SysENUM"},
        { RdevTypes.SysNumber, "RdevTypes.SysNumber"}
    };
}

public class RdevTypeAttribute : System.Attribute
{
    private RdevTypes type;

    public RdevTypeAttribute(RdevTypes type)
    {
        this.type = type;
    }

    public RdevTypes GetType ()
    {
        return this.type;
    }
}

public class RdevTableInfo : System.Attribute
{
    private string tableName;

    public RdevTableInfo(string name)
    {
        this.tableName = name;
    }

    public string GetTableName()
    {
        return this.tableName;
    }
}

/// <summary> 
/// Класс для работы с БД рдева
/// </summary>
public class RdevDatabaseContext
{
    private Npgsql.NpgsqlConnection connection = null;
    private Npgsql.NpgsqlTransaction transaction = null;
    private Newtonsoft.Json.Linq.JToken userInfo = null;

    /// <summary>
    /// Класс для работы с БД рдева
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="transaction"></param>
    /// <param name="userInfo"></param>
    public RdevDatabaseContext(Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction, Newtonsoft.Json.Linq.JToken userInfo)
    {
        this.connection = connection;
        this.transaction = transaction;
        this.userInfo = userInfo;
    }

    /// <summary>
    /// Найти запись по Recid
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="recid"></param>
    /// <returns></returns>
    public T FindByRecid<T>(System.Guid recid) where T : new ()
    {
        return FindByRecidStatic<T>(recid, connection);
    }

    /// <summary>
    /// Найти запись по Recid
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="recid"></param>
    /// <returns></returns>
    private static T FindByRecidStatic<T>(System.Guid recid, Npgsql.NpgsqlConnection connection) where T : new()
    {
        RdevTableInfo tableInfo = (RdevTableInfo)System.Attribute.GetCustomAttribute(typeof(T), typeof(RdevTableInfo));

        if (tableInfo == null)
        {
            throw new FindByRecidException("У таблицы не найден атрибут RdevTableInfo");
        }

        //Выполнение запроса в БД
        using (var command = new Npgsql.NpgsqlCommand($"SELECT * FROM {tableInfo.GetTableName()} WHERE recid = '{recid.ToString()}' AND recstate = 1"))
        {
            command.Connection = connection;

            using (var reader = command.ExecuteReader())
            {
                var recordList = new Newtonsoft.Json.Linq.JArray();

                while (reader.Read())
                {
                    var record = GetDataFromReader(reader);

                    recordList.Add(record);
                }
                if (recordList.Count < 1)
                {
                    throw new FindByRecidException("Запрос в БД по идентификатору в таблицу ХЪ");
                }

                T databaseObject = FillDatabaseObjectProperties<T>(recordList.First, connection);

                return databaseObject;
            }
        }
    }

    /// <summary>
    /// Извлечение данных из записи в БД в JObject
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    private static Newtonsoft.Json.Linq.JObject GetDataFromReader(Npgsql.NpgsqlDataReader reader)
    {
        Newtonsoft.Json.Linq.JObject record = new Newtonsoft.Json.Linq.JObject();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.IsDBNull(i))
            {
                record.Add(reader.GetName(i), null);
            }
            else if (reader.GetValue(i) is System.DateTime)
            {
                record.Add(reader.GetName(i), reader.GetDateTime(i).ToString("o"));
            }
            else if (reader.GetValue(i) is System.Byte[])
            {
                System.IO.Stream stream = reader.GetStream(i);
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    record.Add(reader.GetName(i), memoryStream.ToArray());
                }
            }
            else
            {
                record.Add(reader.GetName(i), reader.GetValue(i).ToString());
            }
        }
        return record;
    }

    /// <summary>
    /// Наполнить объект таблицы данными
    /// </summary>
    /// <param name="resultFromDb"></param>
    /// <returns></returns>
    private static T FillDatabaseObjectProperties<T>(Newtonsoft.Json.Linq.JToken resultFromDb, Npgsql.NpgsqlConnection connection) where T : new()
    {
        T resultObject = new T();
        System.Reflection.PropertyInfo[] properties = resultObject.GetType().GetProperties();

        foreach (System.Reflection.PropertyInfo prop in properties)
        {
            //Получение атрибута JsonProperty
            Newtonsoft.Json.JsonPropertyAttribute[] jsonPropertyAttrs = (Newtonsoft.Json.JsonPropertyAttribute[])System.Attribute.GetCustomAttributes(prop, typeof(Newtonsoft.Json.JsonPropertyAttribute), false);
            if (jsonPropertyAttrs.Length <= 0)
            {
                throw new FillDatabaseObjectPropertiesException($"Не удалось получить атрибуты JsonProperty у свойства '{prop.Name}'");
            }
            Newtonsoft.Json.JsonPropertyAttribute jsonPropertyAttr = jsonPropertyAttrs[0];

            //Получение атрибута RdevTypeAttribute
            RdevTypeAttribute[] rdevTypeAttributes = (RdevTypeAttribute[])System.Attribute.GetCustomAttributes(prop, typeof(RdevTypeAttribute), false);
            if (rdevTypeAttributes.Length <= 0)
            {
                throw new FillDatabaseObjectPropertiesException($"Не удалось получить атрибуты RdevTypeAttribute у свойства '{prop.Name}'");
            }
            RdevTypeAttribute rdevTypeAttribute = rdevTypeAttributes[0];

            switch (rdevTypeAttribute.GetType())
            {
                case RdevTypes.SysString:
                    string propValueSysString = resultFromDb.Value<string>(jsonPropertyAttr.PropertyName);
                    prop.SetValue(resultObject, propValueSysString);
                    break;
                case RdevTypes.SysInt:
                    int? propValueSysInt = resultFromDb.Value<int?>(jsonPropertyAttr.PropertyName);
                    prop.SetValue(resultObject, propValueSysInt);
                    break;
                case RdevTypes.SysRelation:
                    string sysRelationFromDatabase = resultFromDb.Value<string>(jsonPropertyAttr.PropertyName);
                    if (System.String.IsNullOrEmpty(sysRelationFromDatabase))
                    {
                        break;
                    }
                    System.Guid? propValueSysRelation = null;
                    try
                    {
                        propValueSysRelation = System.Guid.Parse(sysRelationFromDatabase);
                    }
                    catch (System.FormatException e)
                    {
                        throw new FillDatabaseObjectPropertiesException($"Не удалось распарсить строку в тип Guid?: {e.Message}");
                    }


                    var propType = prop.GetType();

                    System.Reflection.MethodInfo methodInfo = typeof(RdevDatabaseContext).GetMethod("FindByRecidStatic", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    System.Reflection.MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(propType);

                    prop.SetValue(resultObject, genericMethodInfo.Invoke(null, new object[] { propValueSysRelation, connection }));
                    break;
                case RdevTypes.SysDate:
                    string sysDateFromDatabase = resultFromDb.Value<string>(jsonPropertyAttr.PropertyName);
                    if (System.String.IsNullOrEmpty(sysDateFromDatabase))
                    {
                        break;
                    }
                    System.DateTime? propValueSysDate = System.DateTime.Parse(sysDateFromDatabase);
                    prop.SetValue(resultObject, propValueSysDate);
                    break;
                case RdevTypes.SysTimeDate:
                    string sysTimeDateFromDatabase = resultFromDb.Value<string>(jsonPropertyAttr.PropertyName);
                    if (System.String.IsNullOrEmpty(sysTimeDateFromDatabase))
                    {
                        break;
                    }
                    System.DateTime? propValueSysTimeDate = System.DateTime.Parse(sysTimeDateFromDatabase);
                    prop.SetValue(resultObject, propValueSysTimeDate);
                    break;
                case RdevTypes.SysFile:
                    break;
                case RdevTypes.SysBoolean:
                    bool? propValueSysBoolean = resultFromDb.Value<bool?>(jsonPropertyAttr.PropertyName);
                    prop.SetValue(resultObject, propValueSysBoolean);
                    break;
                case RdevTypes.SysGUID:
                    string sysGuidFromDatabase = resultFromDb.Value<string>(jsonPropertyAttr.PropertyName);
                    if (System.String.IsNullOrEmpty(sysGuidFromDatabase))
                    {
                        break;
                    }
                    System.Guid? propValueSysGUID = null;
                    try
                    {
                        propValueSysGUID = System.Guid.Parse(sysGuidFromDatabase);
                    }
                    catch(System.FormatException e)
                    {
                        throw new FillDatabaseObjectPropertiesException($"Не удалось распарсить строку в тип Guid?: {e.Message}");
                    }

                    prop.SetValue(resultObject, propValueSysGUID);
                    break;
                case RdevTypes.SysENUM:
                    break;
                case RdevTypes.SysNumber:
                    int? propValueSysNumber = resultFromDb.Value<int?>(jsonPropertyAttr.PropertyName);
                    prop.SetValue(resultObject, propValueSysNumber);
                    break;
            }
        }
        return resultObject;
    }

    /// <summary>
    /// Исключение, возникаюзщее в методе FindByRecid
    /// </summary>
    public class FindByRecidException : System.Exception
    {
        public FindByRecidException(string message) : base($"Не удалось получить запись по идентификатору: {message}") { }
    }

    /// <summary>
    /// Исключение, возвращаемое методом FillDatabaseObjectProperties
    /// </summary>
    public class FillDatabaseObjectPropertiesException: System.Exception
    {
        public FillDatabaseObjectPropertiesException (string message) : base($"Не удалось получить атрибуты свойства таблицы: {message}") { }
    }

}
