using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static RDEVDatabaseModelCreator.RdevType;

namespace RDEVDatabaseModelCreator.Classes
{
    public class ModelBuilder
    {
        private List<RdevTable> _tables;

        private CodeCompileUnit modelObject;
        private CodeTypeDeclaration modelClass;
        private MainForm uiForm;

        private Dictionary<string, string> tableNames;
        public ModelBuilder(List<RdevTable> tables, MainForm form)
        {
            _tables = tables;
            modelObject = new CodeCompileUnit();
            
            modelClass = new CodeTypeDeclaration("RdevDatabaseModel");
            modelClass.IsClass = true;
            modelClass.TypeAttributes = TypeAttributes.Public;

            uiForm = form;

        }

        public CodeCompileUnit Build(string nameSpace)
        {
            //Получение классов из файла RdevTypes.cs

            string solutionPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName;
            string rdevTypesPath = Path.Combine(solutionPath, "RDEVDatabaseModelCreator\\RDEVDatabaseModelCreator\\Classes\\RdevSupport.cs");
            CodeSnippetTypeMember rdevTypes = ParseFromFile(rdevTypesPath);

            GenerateTablesNames();
            CodeNamespace codeNamespace = new CodeNamespace(nameSpace);

            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("Newtonsoft.Json"));

            //Получение основной таблицы, от которой будут наследоваться другие
            RdevTable sysBaseTable = _tables.Find(x => x.Name == "SysBaseTable");
            CodeTypeDeclaration sysBaseTableClass = GenerateTableClass(sysBaseTable);
            sysBaseTableClass.Attributes = MemberAttributes.Private;
            modelClass.Members.Add(sysBaseTableClass);
            _tables.Remove(sysBaseTable);

            //Формирование кода для остальных таблиц
            foreach (RdevTable table in _tables)
            {
                CodeTypeDeclaration tableClass = GenerateTableClass(table);
                
                tableClass.BaseTypes.Add(sysBaseTableClass.Name);

                modelClass.Members.Add(tableClass);
            }

            modelClass.Members.Add(rdevTypes);

            codeNamespace.Types.Add(modelClass);
            modelObject.Namespaces.Add(codeNamespace);
            return modelObject;

        }

        /// <summary>
        /// Генерация имен классов таблиц
        /// </summary>
        private void GenerateTablesNames()
        {
            tableNames = new Dictionary<string, string>();
            foreach (RdevTable table in _tables)
            {
                List<string> splittedName = table.Name.Split('_').ToList();
                List<string> updatedName = new List<string>();
                foreach(string name in splittedName)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        //Перевод первой буквы в заглавную
                        char firstChar = char.ToUpper(name[0]);

                        updatedName.Add($"{firstChar}{name.Substring(1)}");
                    }
                }

                tableNames.Add(table.Name, String.Join("", updatedName));
            }
        }

        /// <summary>
        /// Генерация класса таблицы
        /// </summary>
        /// <param name="table">Таблица, для которой генерится класс</param>
        /// <returns></returns>
        private CodeTypeDeclaration GenerateTableClass(RdevTable table)
        {
            string tableName = tableNames[table.Name];

            uiForm.GenerateLogString($"Генерация кода для таблицы '{tableName}'");
            
            CodeTypeDeclaration tableClass = new CodeTypeDeclaration(tableName);
            tableClass.Attributes = MemberAttributes.Public;
            tableClass.CustomAttributes.Add(new CodeAttributeDeclaration("RdevTableInfo", new CodeAttributeArgument(new CodePrimitiveExpression(table.Name))));

            //Создание документационных комментов
            tableClass.Comments.Add(new CodeCommentStatement("<summary>", true));
            if(!String.IsNullOrEmpty(table.DisplayName))
            {
                tableClass.Comments.Add(new CodeCommentStatement($"Модель таблицы '{table.DisplayName}'", true));
            }
            else
            {
                tableClass.Comments.Add(new CodeCommentStatement($"Модель таблицы '{table.Name}'", true));
            }
            tableClass.Comments.Add(new CodeCommentStatement("</summary>", true));

            //Создание свойств таблицы
            List<CodeSnippetTypeMember> fields = CreateTableFields(table.Fields);

            tableClass.Members.AddRange(fields.ToArray());
            return tableClass;
        }

        /// <summary>
        /// Создание свойств таблицы
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        private List<CodeSnippetTypeMember> CreateTableFields(List<RdevField> fields)
        {
            Dictionary<string, string> fieldsNames = GenerateFieldsNames(fields);

            List<CodeSnippetTypeMember> res = new List<CodeSnippetTypeMember>();

            foreach(RdevField field in fields)
            {
                string fieldName = fieldsNames[field.Name];

                uiForm.GenerateLogString($"Генерация кода для поля '{field.Name}', имя в таблице '{fieldName}'");

                CodeSnippetTypeMember generatedField = null;

                //Генерация типа поля
                uiForm.GenerateLogString($"Генерация типа для поля '{field.Name}', имя в таблице '{fieldName}'");

                List<string> customAttributes = new List<string>();

                switch (field.Type)
                {
                    case RdevTypes.SysString:
                        customAttributes.Add($"RdevTypeAttribute({RdevTypesInfo.rdevTypesInfo[RdevTypes.SysString]})");
                        generatedField = new CodeSnippetTypeMember();
                        generatedField.Text = $"\t\t\tpublic string {fieldName} {{ get; set; }}";
                        break;
                    case RdevTypes.SysInt:
                        customAttributes.Add($"RdevTypeAttribute({RdevTypesInfo.rdevTypesInfo[RdevTypes.SysInt]})");
                        generatedField = new CodeSnippetTypeMember();
                        generatedField.Text = $"\t\t\tpublic int? {fieldName} {{ get; set; }}";
                        break;
                    case RdevTypes.SysRelation:
                        customAttributes.Add($"RdevTypeAttribute({RdevTypesInfo.rdevTypesInfo[RdevTypes.SysRelation]})");
                        var relatedTable = field.RelatedTable;
                        var relatedTableName = tableNames[relatedTable.Name];
                        generatedField = new CodeSnippetTypeMember();
                        generatedField.Text = $"\t\t\tpublic {relatedTableName} {fieldName} {{ get; set; }}";
                        break;
                    case RdevTypes.SysDate:
                        customAttributes.Add($"RdevTypeAttribute({RdevTypesInfo.rdevTypesInfo[RdevTypes.SysDate]})");
                        generatedField = new CodeSnippetTypeMember();
                        generatedField.Text = $"\t\t\tpublic DateTime? {fieldName} {{ get; set; }}";
                        break;
                    case RdevTypes.SysTimeDate:
                        customAttributes.Add($"RdevTypeAttribute({RdevTypesInfo.rdevTypesInfo[RdevTypes.SysTimeDate]})");
                        generatedField = new CodeSnippetTypeMember();
                        generatedField.Text = $"\t\t\tpublic DateTime? {fieldName} {{ get; set; }}";
                        break;
                    case RdevTypes.SysFile:
                        customAttributes.Add($"RdevTypeAttribute({RdevTypesInfo.rdevTypesInfo[RdevTypes.SysFile]})");
                        generatedField = new CodeSnippetTypeMember();
                        generatedField.Text = $"\t\t\tpublic string {fieldName} {{ get; set; }}";
                        break;
                    case RdevTypes.SysBoolean:
                        customAttributes.Add($"RdevTypeAttribute({RdevTypesInfo.rdevTypesInfo[RdevTypes.SysBoolean]})");
                        generatedField = new CodeSnippetTypeMember();
                        generatedField.Text = $"\t\t\tpublic bool? {fieldName} {{ get; set; }}";
                        break;
                    case RdevTypes.SysGUID:
                        customAttributes.Add($"RdevTypeAttribute({RdevTypesInfo.rdevTypesInfo[RdevTypes.SysGUID]})");
                        generatedField = new CodeSnippetTypeMember();
                        generatedField.Text = $"\t\t\tpublic Guid? {fieldName} {{ get; set; }}";
                        break;
                    case RdevTypes.SysENUM:
                        customAttributes.Add($"RdevTypeAttribute({RdevTypesInfo.rdevTypesInfo[RdevTypes.SysENUM]})");

                        List<RdevEnumItem> fieldEnumItems = field.Enum;

                        List<string> enumCodeStrings = new List<string>();

                        foreach(RdevEnumItem fieldEnumItem in fieldEnumItems)
                        {
                            enumCodeStrings.Add($"{{{fieldEnumItem.Id}, \"{fieldEnumItem.Value}\"}}");
                        }

                        generatedField = new CodeSnippetTypeMember();
                        generatedField.Text = $"\t\t\tpublic Dictionary<int, string> {fieldName} = new Dictionary<int, string> {{\n{string.Join(", ", enumCodeStrings)}\n}};";
                        break;
                    case RdevTypes.SysNumber:
                        customAttributes.Add($"RdevTypeAttribute({RdevTypesInfo.rdevTypesInfo[RdevTypes.SysNumber]})");
                        generatedField = new CodeSnippetTypeMember();
                        generatedField.Text = $"\t\t\tpublic int? {fieldName} {{ get; set; }}";
                        break;
                }
                //Генерация атрибута
                uiForm.GenerateLogString($"Генерация атрибута [JsonProperty(\"{field.Name}\")] для поля '{field.Name}', имя в таблице '{fieldName}'");
                customAttributes.Add($"JsonProperty(\"{field.Name}\")");

                uiForm.GenerateLogString($"Генерация атрибутов для поля '{field.Name}', имя в таблице '{fieldName}'");

                generatedField.Text = $"\t\t\t[{string.Join(", ", customAttributes)}]\r\n{generatedField.Text}";

                //Создание документационных комментов
                uiForm.GenerateLogString($"Генерация документационных комментов для поля '{field.Name}', имя в таблице '{fieldName}'");
                generatedField.Comments.Add(new CodeCommentStatement("<summary>", true));
                if (!String.IsNullOrEmpty(field.DisplayName))
                {
                    generatedField.Comments.Add(new CodeCommentStatement($"Поле '{field.DisplayName}'", true));
                }
                else
                {
                    generatedField.Comments.Add(new CodeCommentStatement($"Поле '{field.Name}'", true));
                }
                generatedField.Comments.Add(new CodeCommentStatement("</summary>", true));

                res.Add(generatedField);
            }

            return res;
        }

        /// <summary>
        /// Создать имена полей в нотации C#
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        private Dictionary<string, string> GenerateFieldsNames(List<RdevField> fields)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();

            foreach(RdevField field in fields)
            {
                List<string> splittedFieldName = field.Name.Split('_').ToList();
                List<string> updatedFieldName = new List<string>();
                foreach(string name in splittedFieldName)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        //Перевод первой буквы в заглавную
                        char firstChar = char.ToUpper(name[0]);

                        updatedFieldName.Add($"{firstChar}{name.Substring(1)}");
                    }
                }

                res.Add(field.Name, string.Join("", updatedFieldName));
            }

            return res;
        }

        /// <summary>
        /// Получить класс из файла
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private CodeSnippetTypeMember ParseFromFile(string filename)
        {
            string sourceCode = File.OpenText(filename).ReadToEnd();
            return new CodeSnippetTypeMember(sourceCode);
        }
    }
}
