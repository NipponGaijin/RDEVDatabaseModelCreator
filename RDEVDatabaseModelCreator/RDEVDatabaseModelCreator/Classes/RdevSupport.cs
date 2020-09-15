/*
 * Набор объектов, предоставляемых вместе с объектной моделью БД
 * Для работы с объеткной моделью необходимо создать экземпляр класса RdevDatabaseContext
 * все типы наследуются от системной таблицы рдева
 */

/// <summary>
/// Типы рдева
/// </summary>
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
    SysNumber,
    SysDecimal
}

/// <summary>
/// Информация о типах рдева для формирования атрибутов
/// </summary>
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
        { RdevTypes.SysNumber, "RdevTypes.SysNumber"},
        { RdevTypes.SysDecimal, "RdevTypes.SysDecimal" }
    };
}

/// <summary>
/// Атрибут системного типа рдева
/// </summary>
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

/// <summary>
/// Атрибут информации о таблице рдева 
/// </summary>
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
/// Контейнер для параметров insert запроса
/// </summary>
/// <typeparam name="T"></typeparam>
public class InsertQueryParams<T>
{
    public string Keys { get; set; }
    public string Values { get; set; }
    public System.Collections.Generic.List<System.Data.SqlClient.SqlParameter> SqlParameters { get; set; }

    public T InsertObject {get; set;}

    public InsertQueryParams(System.Collections.Generic.List<System.String> keys, System.Collections.Generic.List<System.String> values, System.Collections.Generic.List<System.Data.SqlClient.SqlParameter> sqlParameters, T insertObject)
    {
        this.Keys = System.String.Join(", ", keys);
        this.Values = System.String.Join(", ", values);
        this.SqlParameters = sqlParameters;
        this.InsertObject = insertObject;
    }
}

/// <summary>
/// Контейнер для параметров update запроса
/// </summary>
/// <typeparam name="T"></typeparam>
public class UpdateQueryParams<T>
{
    public System.Guid RecId { get; set; }
    public string QueryParams { get; set; }
    public System.Collections.Generic.List<System.Data.SqlClient.SqlParameter> SqlParameters { get; set; }

    public T UpdateObject { get; set; }

    public UpdateQueryParams(System.Guid recId, System.Collections.Generic.List<string> queryParams, System.Collections.Generic.List<System.Data.SqlClient.SqlParameter> sqlParameters, T updateObject)
    {
        RecId = recId;
        QueryParams = string.Join(", ", queryParams);
        SqlParameters = sqlParameters;
        UpdateObject = updateObject;
    }
}

/// <summary>
/// Тип сортировки в запросах
/// </summary>
public enum OrderByType
{
    Asc = 0,
    Desc = 1
}

/// <summary> 
/// Класс для работы с БД рдева
/// </summary>
public class RdevDatabaseContext
{
    public Npgsql.NpgsqlConnection Connection { get; } = null;
    public Npgsql.NpgsqlTransaction Transaction { get; } = null;
    public Newtonsoft.Json.Linq.JToken UserInfo { get; } = null;
    /// <summary>
    /// Класс для работы с БД рдева
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="transaction"></param>
    /// <param name="userInfo"></param>
    public RdevDatabaseContext(Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction, Newtonsoft.Json.Linq.JToken userInfo = null)
    {
        this.Connection = connection;
        this.Transaction = transaction;
        this.UserInfo = userInfo;
    }

    /// <summary>
    /// Найти запись по Recid
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="recid"></param>
    /// <returns></returns>
    public T FindByRecid<T>(System.Guid recid) where T : new()
    {
        return FindByRecidStatic<T>(recid, Connection);
    }

    /// <summary>
    /// Найти запись по выражению Where
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="whereStatement"></param>
    /// <returns></returns>
    public System.Collections.Generic.List<T> FindByParams<T>(System.String whereStatement) where T : new()
    {
        RdevTableInfo tableInfo = (RdevTableInfo)System.Attribute.GetCustomAttribute(typeof(T), typeof(RdevTableInfo));

        var recordList = new Newtonsoft.Json.Linq.JArray();
        System.Collections.Generic.List<T> result = new System.Collections.Generic.List<T>();
        using (var command = new Npgsql.NpgsqlCommand($"SELECT * FROM {tableInfo.GetTableName()} WHERE {whereStatement} AND recstate = 1"))
        {
            command.Connection = this.Connection;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var record = GetDataFromReader(reader);

                    recordList.Add(record);
                }
            }
        }
        
        if(recordList.Count <= 0)
        {
            return result;
        }

        foreach(Newtonsoft.Json.Linq.JToken record in recordList)
        {
            try
            {
                T databaseObject = FillDatabaseObjectProperties<T>(record, Connection);
                result.Add(databaseObject);
            } 
            catch (FillDatabaseObjectPropertiesException e)
            {
                throw new FindByParamsException(e.Message);
            }
        }
        return result;
    }

    /// <summary>
    /// Найти записи по выражению
    /// </summary>
    /// <typeparam name="T">Тип записи</typeparam>
    /// <param name="whereStatement">Выражение по которому происходит выборка</param>
    /// <param name="orderByField">Название поля, по которому идет сортировка</param>
    /// <param name="orderBy">Тип сортировки</param>
    /// <returns></returns>
    public System.Collections.Generic.List<T> FindByParams<T>(System.String whereStatement, System.String orderByField, OrderByType orderBy) where T : new()
    {
        string orderByStatement = orderBy == 0 ? $"{orderByField} ASC" : $"{orderByField} DESC";
        //Получение данных из талицы
        RdevTableInfo tableInfo = (RdevTableInfo)System.Attribute.GetCustomAttribute(typeof(T), typeof(RdevTableInfo));

        var recordList = new Newtonsoft.Json.Linq.JArray();
        System.Collections.Generic.List<T> result = new System.Collections.Generic.List<T>();
        using (var command = new Npgsql.NpgsqlCommand($"SELECT * FROM {tableInfo.GetTableName()} WHERE {whereStatement} AND recstate = 1 ORDER BY {orderByStatement}"))
        {
            command.Connection = this.Connection;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var record = GetDataFromReader(reader);

                    recordList.Add(record);
                }
            }
        }

        if (recordList.Count <= 0)
        {
            return result;
        }

        foreach (Newtonsoft.Json.Linq.JToken record in recordList)
        {
            try
            {
                T databaseObject = FillDatabaseObjectProperties<T>(record, Connection);
                result.Add(databaseObject);
            }
            catch (FillDatabaseObjectPropertiesException e)
            {
                throw new FindByParamsException(e.Message);
            }
        }
        return result;
    }

    /// <summary>
    /// Создание записи в БД
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="insertObject"></param>
    /// <returns></returns>
    public T Insert<T>(T insertObject)
    {
        RdevTableInfo tableInfo = (RdevTableInfo)System.Attribute.GetCustomAttribute(typeof(T), typeof(RdevTableInfo));
        if(tableInfo == null)
        {
            throw new InsertException("Не удалось получить информацию о таблице");
        }

        InsertQueryParams<T> insertQueryParams = PrepareInsertQueryParams(insertObject);

        try
        {
            using (var command = new Npgsql.NpgsqlCommand($"INSERT INTO {tableInfo.GetTableName()} ({insertQueryParams.Keys}) VALUES ({insertQueryParams.Values})"))
            {
                if (insertQueryParams.SqlParameters.Count > 0)
                {
                    foreach (System.Data.SqlClient.SqlParameter parameter in insertQueryParams.SqlParameters)
                    {
                        command.Parameters.AddWithValue(parameter.ParameterName, parameter.Value);
                    }
                }
                command.Connection = this.Connection;
                command.Transaction = this.Transaction;
                int rowsCount = command.ExecuteNonQuery();
                return insertQueryParams.InsertObject;
            }
        }
        catch (System.Exception e)
        {
            throw new InsertException(e.Message);
        }
    }

    /// <summary>
    /// Обновление записи в БД
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="updateObject">Объект для обновления</param>
    /// <returns></returns>
    public T Update<T>(T updateObject)
    {
        System.Reflection.PropertyInfo[] properties = typeof(T).GetProperties();
        foreach(var prop in properties)
        {
            //Получение атрибута JsonProperty
            Newtonsoft.Json.JsonPropertyAttribute[] propJsonAttrs = (Newtonsoft.Json.JsonPropertyAttribute[])System.Attribute.GetCustomAttributes(prop, typeof(Newtonsoft.Json.JsonPropertyAttribute), false);
            if (propJsonAttrs.Length <= 0)
            {
                throw new UpdateException($"Не удалось получить атрибуты JsonProperty у свойства '{prop.Name}'");
            }
            Newtonsoft.Json.JsonPropertyAttribute propJsonAttr = propJsonAttrs[0];

            if(propJsonAttr.PropertyName == "recid")
            {
                object propValue = prop.GetValue(updateObject, null);
                if(propValue != null)
                {
                    return Update(updateObject, (System.Guid)propValue);
                }
                else
                {
                    throw new UpdateException("Свойство Recid не может быть равным null");
                }
            }
        }
        throw new UpdateException("Не удалось получить свойство Recid обновляемого объекта");
    }

    /// <summary>
    /// Обновление записи в БД
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="updateObject">Объект для обновления</param>
    /// <param name="recordId">Идентификатор записи длят обновления</param>
    /// <returns></returns>
    public T Update<T>(T updateObject, System.Guid recordId)
    {
        RdevTableInfo tableInfo = (RdevTableInfo)System.Attribute.GetCustomAttribute(typeof(T), typeof(RdevTableInfo));
        if (tableInfo == null)
        {
            throw new UpdateException("Не удалось получить информацию о таблице");
        }

        UpdateQueryParams<T> updateQueryParams = PrepareUpdateQueryParams(updateObject);

        try
        {
            using (var command = new Npgsql.NpgsqlCommand($"UPDATE {tableInfo.GetTableName()} SET {updateQueryParams.QueryParams} WHERE recid = '{recordId.ToString()}'"))
            {
                command.Connection = this.Connection;
                command.Transaction = this.Transaction;
                int rowsCount = command.ExecuteNonQuery();
                return updateQueryParams.UpdateObject;
            }
        }
        catch (System.Exception e)
        {
            throw new UpdateException(e.Message);
        }
    }

    /// <summary>
    /// Удаление записи из БД
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="deleteObject"></param>
    /// <returns></returns>
    public int Delete<T>(T deleteObject)
    {
        RdevTableInfo tableInfo = (RdevTableInfo)System.Attribute.GetCustomAttribute(typeof(T), typeof(RdevTableInfo));
        if (tableInfo == null)
        {
            throw new DeleteException("Не удалось получить информацию о таблице");
        }

        System.Reflection.PropertyInfo[] properties = typeof(T).GetProperties();
        foreach (var prop in properties)
        {
            //Получение атрибута JsonProperty
            Newtonsoft.Json.JsonPropertyAttribute[] propJsonAttrs = (Newtonsoft.Json.JsonPropertyAttribute[])System.Attribute.GetCustomAttributes(prop, typeof(Newtonsoft.Json.JsonPropertyAttribute), false);
            if (propJsonAttrs.Length <= 0)
            {
                throw new DeleteException($"Не удалось получить атрибуты JsonProperty у свойства '{prop.Name}'");
            }
            Newtonsoft.Json.JsonPropertyAttribute propJsonAttr = propJsonAttrs[0];

            if (propJsonAttr.PropertyName == "recid")
            {
                object propValue = prop.GetValue(deleteObject, null);
                if (propValue != null)
                {
                    try
                    {
                        using (var command = new Npgsql.NpgsqlCommand($"DELETE FROM {tableInfo.GetTableName()} WHERE recid = '{propValue}'"))
                        {
                            command.Connection = this.Connection;
                            command.Transaction = this.Transaction;
                            int rowsCount = command.ExecuteNonQuery();
                            return rowsCount;
                        }
                    }
                    catch (System.Exception e)
                    {
                        throw new DeleteException(e.Message);
                    }
                }
                else
                {
                    throw new DeleteException("Свойство Recid не может быть равным null");
                }
            }
        }
        throw new DeleteException("Не удалось получить свойство Recid обновляемого объекта");
    }

    /// <summary>
    /// Генерация параметров insert запроса
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="insertObject"></param>
    /// <returns></returns>
    private InsertQueryParams<T> PrepareInsertQueryParams<T>(T insertObject)
    {
        System.Collections.Generic.List<string> keys = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> values = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<System.Data.SqlClient.SqlParameter> queryParams = new System.Collections.Generic.List<System.Data.SqlClient.SqlParameter>();

        //Получение свойств объекта
        System.Reflection.PropertyInfo[] properties = typeof(T).GetProperties();

        foreach(var prop in properties)
        {
            //Получение атрибута JsonProperty
            Newtonsoft.Json.JsonPropertyAttribute[] jsonPropertyAttrs = (Newtonsoft.Json.JsonPropertyAttribute[])System.Attribute.GetCustomAttributes(prop, typeof(Newtonsoft.Json.JsonPropertyAttribute), false);
            if (jsonPropertyAttrs.Length <= 0)
            {
                throw new InsertException($"Не удалось получить атрибуты JsonProperty у свойства '{prop.Name}'");
            }
            Newtonsoft.Json.JsonPropertyAttribute jsonPropertyAttr = jsonPropertyAttrs[0];
            string fieldName = jsonPropertyAttr.PropertyName.ToLower();

            //Получение атрибута RdevTypeAttribute
            RdevTypeAttribute[] rdevTypeAttributes = (RdevTypeAttribute[])System.Attribute.GetCustomAttributes(prop, typeof(RdevTypeAttribute), false);
            if (rdevTypeAttributes.Length <= 0)
            {
                throw new InsertException($"Не удалось получить атрибуты RdevTypeAttribute у свойства '{prop.Name}'");
            }
            RdevTypeAttribute rdevTypeAttribute = rdevTypeAttributes[0];

            //Наполнение системных полей
            switch (fieldName)
            {
                case "recid":
                    object propRecIdValue = prop.GetValue(insertObject, null);
                    if (propRecIdValue == null)
                    {
                        prop.SetValue(insertObject, System.Guid.NewGuid());
                    }
                    break;
                case "reccreatedby":
                    if(UserInfo != null)
                    {
                        prop.SetValue(insertObject, UserInfo.Value<string>("recid"));
                    }
                    break;
                case "recupdatedby":
                    prop.SetValue(insertObject, UserInfo.Value<string>("recid"));
                    break;
                case "reccreated":
                    prop.SetValue(insertObject, System.DateTime.UtcNow);
                    break;
                case "recupdated":
                    prop.SetValue(insertObject, System.DateTime.UtcNow);
                    break;
                case "recstate":
                    prop.SetValue(insertObject, 1);
                    break;
            }

            if (fieldName.Length >= 64)
            {
                fieldName = $"\"{fieldName.Substring(0, 62)}~\"";
            }

            switch (rdevTypeAttribute.GetType())
            {
                case RdevTypes.SysString:
                    object propValueSysString = prop.GetValue(insertObject, null);

                    keys.Add(fieldName);
                    if(propValueSysString == null)
                    {
                        values.Add("NULL");
                    }
                    else
                    {
                        values.Add($"'{propValueSysString}'");
                    }

                    break;
                case RdevTypes.SysInt:
                    object propValueSysInt = prop.GetValue(insertObject, null);

                    keys.Add(fieldName);
                    if (propValueSysInt == null)
                    {
                        values.Add("NULL");
                    }
                    else
                    {
                        values.Add($"'{propValueSysInt}'");
                    }
                    break;
                case RdevTypes.SysRelation:
                    System.Reflection.PropertyInfo[] relatedFieldProps = prop.PropertyType.GetProperties();
                    foreach (var relatedFieldProp in relatedFieldProps)
                    {
                        object relatedFieldValue = prop.GetValue(insertObject, null);
                        if (relatedFieldValue != null)
                        {
                            //Получение атрибута JsonProperty
                            Newtonsoft.Json.JsonPropertyAttribute[] relatedFieldPropJsonPropertyAttrs = (Newtonsoft.Json.JsonPropertyAttribute[])System.Attribute.GetCustomAttributes(relatedFieldProp, typeof(Newtonsoft.Json.JsonPropertyAttribute), false);
                            if (jsonPropertyAttrs.Length <= 0)
                            {
                                throw new InsertException($"Не удалось получить атрибуты JsonProperty у свойства '{relatedFieldProp.Name}'");
                            }
                            Newtonsoft.Json.JsonPropertyAttribute relatedFieldPropJsonPropertyAttr = relatedFieldPropJsonPropertyAttrs[0];

                            if (relatedFieldPropJsonPropertyAttr.PropertyName.ToLower() == "recid")
                            {
                                keys.Add(fieldName);

                                object propValueSysRelation = relatedFieldProp.GetValue(relatedFieldValue, null);
                                if (propValueSysRelation != null)
                                {
                                    values.Add($"'{propValueSysRelation}'");
                                    break;
                                }
                            }
                        }
                    }
                    break;
                case RdevTypes.SysDate:
                    object propValueSysDate = prop.GetValue(insertObject, null);

                    keys.Add(fieldName);
                    if (propValueSysDate == null)
                    {
                        values.Add("NULL");
                    }
                    else
                    {
                        values.Add($"'{propValueSysDate}'");
                    }
                    break;
                case RdevTypes.SysTimeDate:
                    object propValueSysTimeDate = prop.GetValue(insertObject, null);

                    keys.Add(fieldName);
                    if (propValueSysTimeDate == null)
                    {
                        values.Add("NULL");
                    }
                    else
                    {
                        values.Add($"'{propValueSysTimeDate}'");
                    }
                    break;
                case RdevTypes.SysFile:
                    break;
                case RdevTypes.SysBoolean:
                    object propValueSysBoolean = prop.GetValue(insertObject, null);

                    keys.Add(fieldName);
                    if (propValueSysBoolean == null)
                    {
                        values.Add("NULL");
                    }
                    else
                    {
                        values.Add($"'{propValueSysBoolean}'");
                    }
                    break;
                case RdevTypes.SysGUID:
                    object propValueSysGUID = prop.GetValue(insertObject, null);

                    keys.Add(fieldName);
                    if (propValueSysGUID == null)
                    {
                        values.Add("NULL");
                    }
                    else
                    {
                        values.Add($"'{propValueSysGUID}'");
                    }
                    break;
                case RdevTypes.SysENUM:
                    break;
                case RdevTypes.SysNumber:
                    object propValueSysNumber = prop.GetValue(insertObject, null);

                    keys.Add(fieldName);
                    if (propValueSysNumber == null)
                    {
                        values.Add("NULL");
                    }
                    else
                    {
                        values.Add($"'{propValueSysNumber}'");
                    }
                    break;
                case RdevTypes.SysDecimal:
                    object propValueSysDecimal = prop.GetValue(insertObject, null);
                    keys.Add(fieldName);
                    if (propValueSysDecimal == null)
                    {
                        values.Add("NULL");
                    }
                    else
                    {
                        values.Add($"'{propValueSysDecimal}'");
                    }
                    break;
            }
        }

        InsertQueryParams<T> res = new InsertQueryParams<T>(keys, values, queryParams, insertObject);
        return res;
    }

    /// <summary>
    /// Подготовка параметров для Update запроса
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="updateObject"></param>
    /// <returns></returns>
    private UpdateQueryParams<T> PrepareUpdateQueryParams<T>(T updateObject)
    {
        System.Collections.Generic.List<string> queryParams = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<System.Data.SqlClient.SqlParameter> updateParams = new System.Collections.Generic.List<System.Data.SqlClient.SqlParameter>();
        System.Guid updateRecordRecid = System.Guid.Empty;

        //Получение свойств объекта
        System.Reflection.PropertyInfo[] properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            //Получение атрибута JsonProperty
            Newtonsoft.Json.JsonPropertyAttribute[] jsonPropertyAttrs = (Newtonsoft.Json.JsonPropertyAttribute[])System.Attribute.GetCustomAttributes(prop, typeof(Newtonsoft.Json.JsonPropertyAttribute), false);
            if (jsonPropertyAttrs.Length <= 0)
            {
                throw new UpdateException($"Не удалось получить атрибуты JsonProperty у свойства '{prop.Name}'");
            }
            Newtonsoft.Json.JsonPropertyAttribute jsonPropertyAttr = jsonPropertyAttrs[0];
            string fieldName = jsonPropertyAttr.PropertyName.ToLower();

            //Получение атрибута RdevTypeAttribute
            RdevTypeAttribute[] rdevTypeAttributes = (RdevTypeAttribute[])System.Attribute.GetCustomAttributes(prop, typeof(RdevTypeAttribute), false);
            if (rdevTypeAttributes.Length <= 0)
            {
                throw new UpdateException($"Не удалось получить атрибуты RdevTypeAttribute у свойства '{prop.Name}'");
            }
            RdevTypeAttribute rdevTypeAttribute = rdevTypeAttributes[0];

            //Получение и обновление системных полей
            switch (fieldName)
            {
                case "recid":
                    object propRecIdValue = prop.GetValue(updateObject, null);
                    if (propRecIdValue == null)
                    {
                        throw new UpdateException("В обновляемом объекте не заполнено свойство RecId");
                    }
                    else
                    {
                        updateRecordRecid = (System.Guid)propRecIdValue;
                    }
                    break;
                case "recupdatedby":
                    if (UserInfo != null)
                    {
                        prop.SetValue(updateObject, UserInfo.Value<string>("recid"));
                    }
                    break;
                case "recupdated":
                    prop.SetValue(updateObject, System.DateTime.UtcNow);
                    break;
            }

            //Если длина имени поля больше 64х символов, она укорачивается и добавляется тильда в конце
            if (fieldName.Length >= 64)
            {
                fieldName = $"\"{fieldName.Substring(0, 62)}~\"";
            }


            switch (rdevTypeAttribute.GetType())
            {
                case RdevTypes.SysString:
                    object propValueSysString = prop.GetValue(updateObject, null);

                    if (propValueSysString == null)
                    {
                        queryParams.Add($"{fieldName} = NULL");
                    }
                    else
                    {
                        queryParams.Add($"{fieldName} = '{propValueSysString}'");
                    }

                    break;
                case RdevTypes.SysInt:
                    object propValueSysInt = prop.GetValue(updateObject, null);

                    if (propValueSysInt == null)
                    {
                        queryParams.Add($"{fieldName} = NULL");
                    }
                    else
                    {
                        queryParams.Add($"{fieldName} = '{propValueSysInt}'");
                    }
                    break;
                case RdevTypes.SysRelation:
                    System.Reflection.PropertyInfo[] relatedFieldProps = prop.PropertyType.GetProperties();
                    foreach (var relatedFieldProp in relatedFieldProps)
                    {
                        object relatedFieldValue = prop.GetValue(updateObject, null);
                        if (relatedFieldValue != null)
                        {
                            //Получение атрибута JsonProperty
                            Newtonsoft.Json.JsonPropertyAttribute[] relatedFieldPropJsonPropertyAttrs = (Newtonsoft.Json.JsonPropertyAttribute[])System.Attribute.GetCustomAttributes(relatedFieldProp, typeof(Newtonsoft.Json.JsonPropertyAttribute), false);
                            if (jsonPropertyAttrs.Length <= 0)
                            {
                                throw new UpdateException($"Не удалось получить атрибуты JsonProperty у свойства '{relatedFieldProp.Name}'");
                            }
                            Newtonsoft.Json.JsonPropertyAttribute relatedFieldPropJsonPropertyAttr = relatedFieldPropJsonPropertyAttrs[0];

                            if (relatedFieldPropJsonPropertyAttr.PropertyName.ToLower() == "recid")
                            {

                                object propValueSysRelation = relatedFieldProp.GetValue(relatedFieldValue, null);
                                if (propValueSysRelation != null)
                                {
                                    queryParams.Add($"{jsonPropertyAttr.PropertyName.ToLower()} = '{propValueSysRelation}'");
                                    break;
                                }
                            }
                        }
                    }
                    break;
                case RdevTypes.SysDate:
                    object propValueSysDate = prop.GetValue(updateObject, null);

                    if (propValueSysDate == null)
                    {
                        queryParams.Add($"{fieldName} = NULL");
                    }
                    else
                    {
                        queryParams.Add($"{fieldName} = '{propValueSysDate}'");
                    }
                    break;
                case RdevTypes.SysTimeDate:
                    object propValueSysTimeDate = prop.GetValue(updateObject, null);

                    if (propValueSysTimeDate == null)
                    {
                        queryParams.Add($"{fieldName} = NULL");
                    }
                    else
                    {
                        queryParams.Add($"{fieldName} = '{propValueSysTimeDate}'");
                    }
                    break;
                case RdevTypes.SysFile:
                    break;
                case RdevTypes.SysBoolean:
                    object propValueSysBoolean = prop.GetValue(updateObject, null);

                    if (propValueSysBoolean == null)
                    {
                        queryParams.Add($"{fieldName} = NULL");
                    }
                    else
                    {
                        queryParams.Add($"{fieldName} = '{propValueSysBoolean}'");
                    }
                    break;
                case RdevTypes.SysGUID:
                    object propValueSysGUID = prop.GetValue(updateObject, null);

                    if (propValueSysGUID == null)
                    {
                        queryParams.Add($"{fieldName} = NULL");
                    }
                    else
                    {
                        queryParams.Add($"{fieldName} = '{propValueSysGUID}'");
                    }
                    break;
                case RdevTypes.SysENUM:
                    break;
                case RdevTypes.SysNumber:
                    object propValueSysNumber = prop.GetValue(updateObject, null);

                    if (propValueSysNumber == null)
                    {
                        queryParams.Add($"{fieldName} = NULL");
                    }
                    else
                    {
                        queryParams.Add($"{fieldName} = '{propValueSysNumber}'");
                    }
                    break;
                case RdevTypes.SysDecimal:
                    object propValueSysDecimal = prop.GetValue(updateObject, null);

                    if (propValueSysDecimal == null)
                    {
                        queryParams.Add($"{fieldName} = NULL");
                    }
                    else
                    {
                        queryParams.Add($"{fieldName} = '{propValueSysDecimal}'");
                    }
                    break;
            }
        }
        UpdateQueryParams<T> res = new UpdateQueryParams<T>(updateRecordRecid, queryParams, updateParams, updateObject);
        return res;
    }

    /// <summary>
    /// Найти запись по Recid
    /// </summary>
    /// <typeparam name="T">Тип объекта таблицы</typeparam>
    /// <param name="recid">Идентификатор записи в таблице</param>
    /// <returns></returns>
    private static T FindByRecidStatic<T>(System.Guid recid, Npgsql.NpgsqlConnection connection) where T : new()
    {
        RdevTableInfo tableInfo = (RdevTableInfo)System.Attribute.GetCustomAttribute(typeof(T), typeof(RdevTableInfo));

        if (tableInfo == null)
        {
            throw new FindByRecidException("У таблицы не найден атрибут RdevTableInfo");
        }
        var recordsList = new Newtonsoft.Json.Linq.JArray();
        //Выполнение запроса в БД
        using (var command = new Npgsql.NpgsqlCommand($"SELECT * FROM {tableInfo.GetTableName()} WHERE recid = '{recid.ToString()}' AND recstate = 1"))
        {
            command.Connection = connection;

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var record = GetDataFromReader(reader);

                    recordsList.Add(record);
                }
                if (recordsList.Count > 1)
                {
                    throw new FindByRecidException($"Запрос в БД по идентификатору в таблицу '{tableInfo.GetTableName()}' вернул больше одной записи.");
                }
                if(recordsList.Count <= 0)
                {
                    return default(T);
                }
            }
        }

        try
        {
            T databaseObject = FillDatabaseObjectProperties<T>(recordsList.First, connection);

            return databaseObject;
        }
        catch(FillDatabaseObjectPropertiesException e)
        {
            throw new FindByRecidException(e.Message);
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
        System.Type databaseObjectType = typeof(T);

        System.Reflection.PropertyInfo[] properties = databaseObjectType.GetProperties();

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
                    string propValueSysString = resultFromDb.Value<string>(jsonPropertyAttr.PropertyName.ToLower());
                    prop.SetValue(resultObject, propValueSysString);
                    break;
                case RdevTypes.SysInt:
                    int? propValueSysInt = resultFromDb.Value<int?>(jsonPropertyAttr.PropertyName.ToLower());
                    prop.SetValue(resultObject, propValueSysInt);
                    break;
                case RdevTypes.SysRelation:
                    string sysRelationFromDatabase = resultFromDb.Value<string>(jsonPropertyAttr.PropertyName.ToLower());
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

                    var propType = prop.PropertyType;

                    System.Reflection.MethodInfo methodInfo = typeof(RdevDatabaseContext).GetMethod("FindByRecidStatic", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    System.Reflection.MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(propType);

                    prop.SetValue(resultObject, genericMethodInfo.Invoke(null, new object[] { propValueSysRelation, connection }));
                    break;
                case RdevTypes.SysDate:
                    string sysDateFromDatabase = resultFromDb.Value<string>(jsonPropertyAttr.PropertyName.ToLower());
                    if (System.String.IsNullOrEmpty(sysDateFromDatabase))
                    {
                        break;
                    }
                    System.DateTime? propValueSysDate = System.DateTime.Parse(sysDateFromDatabase);
                    prop.SetValue(resultObject, propValueSysDate);
                    break;
                case RdevTypes.SysTimeDate:
                    string sysTimeDateFromDatabase = resultFromDb.Value<string>(jsonPropertyAttr.PropertyName.ToLower());
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
                    bool? propValueSysBoolean = resultFromDb.Value<bool?>(jsonPropertyAttr.PropertyName.ToLower());
                    prop.SetValue(resultObject, propValueSysBoolean);
                    break;
                case RdevTypes.SysGUID:
                    string sysGuidFromDatabase = resultFromDb.Value<string>(jsonPropertyAttr.PropertyName.ToLower());
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
                    int? propValueSysNumber = resultFromDb.Value<int?>(jsonPropertyAttr.PropertyName.ToLower());
                    prop.SetValue(resultObject, propValueSysNumber);
                    break;
                case RdevTypes.SysDecimal:
                    float? propValueSysDecimal = resultFromDb.Value<float?>(jsonPropertyAttr.PropertyName.ToLower());
                    prop.SetValue(resultObject, propValueSysDecimal);
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
    /// Исключение, возникающее в методе FindByParams
    /// </summary>
    public class FindByParamsException : System.Exception
    {
        public FindByParamsException(string message): base($"Не удалось получить записи по параметрам: {message}") { }
    }

    /// <summary>
    /// Исключение, возвращаемое методом FillDatabaseObjectProperties
    /// </summary>
    public class FillDatabaseObjectPropertiesException: System.Exception
    {
        public FillDatabaseObjectPropertiesException (string message) : base($"Не удалось получить атрибуты свойства таблицы: {message}") { }
    }

    /// <summary>
    /// Исключение, возвращаемое методом INSERT
    /// </summary>
    public class InsertException : System.Exception
    {
        public InsertException(string message) : base($"Не удалось запись в таблице: {message}") { }
    }

    /// <summary>
    /// Исключение, возвращаемое методом UPDATE
    /// </summary>
    public class UpdateException : System.Exception
    {
        public UpdateException(string message) : base($"Не удалось обновить запись в таблице: {message}") { }
    }

    /// <summary>
    /// Исключение, возвращаемое методом DELETE
    /// </summary>
    public class DeleteException : System.Exception
    {
        public DeleteException(string message): base ($"Не удалось удалить запись из БД: {message}") { }
    }
}
