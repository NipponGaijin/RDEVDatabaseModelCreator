using Mono.CSharp;
using Newtonsoft.Json.Linq;
using RDEVDatabaseModelCreator.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RDEVDatabaseModelCreator
{
    public partial class MainForm : Form
    {
        JObject _projectFile;
        JObject _sysProjectFile;
        string _outputFolderPath;
        string _openedFolder;

        string[] exceptionTableNames = {
            "RDEV___Auth_Data_Policies"
        };

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }


        /// <summary>
        /// Создать строку лога и записать ее в форму лога
        /// </summary>
        /// <param name="message"></param>
        private void GenerateLogString(string message)
        {
            infoTxt.AppendText($"{DateTime.Now} || {message}{Environment.NewLine}");
        }

        /// <summary>
        /// Кнопка "Открыть"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.Filter = "Проектные файлы JSON (usr.json)|usr.json";

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    var path = fileDialog.FileName;
                    openedFolderTxt.Text = path;
                    _openedFolder = Path.GetDirectoryName(path);
                    string fileContent = File.ReadAllText(path);
                    if (fileContent != null)
                    {
                        try
                        {
                            _projectFile = JObject.Parse(fileContent);
                            GenerateLogString($"Файл '{path}' успешно разобран.");
                        }
                        catch (Exception ex)
                        {
                            GenerateLogString($"Не удалось распарсить содержимое файла: {ex.Message}");
                        }
                    }
                    else
                    {
                        GenerateLogString("Не удалось распарсить содержимое файла в JSON так как он пустой.");
                    }
                }
            }

            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.Filter = "Проектные файлы JSON (sys.json)|sys.json";

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    var path = fileDialog.FileName;
                    string fileContent = File.ReadAllText(path);
                    if (fileContent != null)
                    {
                        try
                        {
                            _sysProjectFile = JObject.Parse(fileContent);
                            GenerateLogString($"Файл '{path}' успешно разобран.");
                        }
                        catch (Exception ex)
                        {
                            GenerateLogString($"Не удалось распарсить содержимое файла: {ex.Message}");
                        }
                    }
                    else
                    {
                        GenerateLogString("Не удалось распарсить содержимое файла в JSON так как он пустой.");
                    }
                }
            }
        }

        /// <summary>
        /// Кнопка "Сохранить"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveFileMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog folderDialog = new OpenFileDialog())
            {
                if(_openedFolder != null)
                {
                    folderDialog.InitialDirectory = _openedFolder;
                }

                folderDialog.ValidateNames = false;
                folderDialog.CheckFileExists = false;
                folderDialog.CheckPathExists = true;
                folderDialog.FileName = "Folder Selection.";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    _outputFolderPath = Path.GetDirectoryName(folderDialog.FileName);
                    openedFolderTxt.Text = Path.GetDirectoryName(folderDialog.FileName);
                    GenerateLogString($"Выбрана директория: {_outputFolderPath}");
                }
            }
        }

        /// <summary>
        /// Кнопка "Сформировать объектную модель"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buildObjectModelBtn_Click(object sender, EventArgs e)
        {
            //Валидации
            if(_projectFile == null)
            {
                MessageBox.Show("Проектный файл не загружен!");
                GenerateLogString($"Проектный файл не загружен!");
                return;
            }

            if(_sysProjectFile == null)
            {
                MessageBox.Show("Системный проектный файл не загружен!");
                GenerateLogString($"Системный проектный файл не загружен!");
                return;
            }

            if(String.IsNullOrEmpty(_outputFolderPath) || String.IsNullOrWhiteSpace(_outputFolderPath)) 
            {
                MessageBox.Show("Не выбрана директория для генерации объектной модели!");
                GenerateLogString($"Не выбрана директория для генерации объектной модели!");
                return;
            }

            //Формирование объектной модели
            BuildObjectModel();
        }

        /// <summary>
        /// Формирование объектной модели
        /// </summary>
        private void BuildObjectModel()
        {
            _projectFile.Merge(_sysProjectFile);
            //Получение таблиц и типов из проектного файла
            JToken tables = _projectFile["tables"];
            if(tables == null)
            {
                GenerateLogString($"В загруженном файле не найдено описание таблиц RDEV!");
                return;
            }
            JToken types = _projectFile["types"];
            if(types == null)
            {
                GenerateLogString($"В загруженном файле не найдено описание типов RDEV!");
                return;
            }

            //Получение таблиц и типов из системного проектного файла

            JToken sysTables = _sysProjectFile["tables"];
            if(sysTables == null)
            {
                GenerateLogString($"В загруженном системном проектном файле не найдено описание таблиц RDEV!");
                return;
            }

            JToken sysTypes = _sysProjectFile["types"];
            if (sysTypes == null)
            {
                GenerateLogString($"В загруженном системном проектном файле не найдено описание типов RDEV!");
                return;
            }


            List<RdevTable> rdevTables = ProcessRdevTables(tables, types);
            if(rdevTables != null)
            {
                GenerateLogString($"Таблицы успешно обработаны");
            }

        }

        private List<RdevTable> ProcessRdevTables(JToken tables, JToken types)
        {
            List<RdevTable> res = new List<RdevTable>();

            foreach (JToken table in tables)
            {
                if(res.Find(x => x.Name.ToLower() == table.Value<string>("name")) == null)
                {
                    List<RdevTable> rdevTables = ProcessRdevTable(table, tables, types);
                    if (rdevTables != null)
                    {
                        foreach(var rdevTable in rdevTables)
                        {
                            if (res.Find(x => (x.Name ?? "").ToLower() == (rdevTable.Name ?? "").ToLower()) == null)
                            {
                                res.Add(rdevTable);
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                    
                }
            }
            return res;
        }

        private List<RdevTable> ProcessRdevTable(JToken table, JToken tables, JToken types)
        {
            List<RdevTable> rdevTables = new List<RdevTable>();

            JToken fields = table["fields"];
            if (fields == null)
            {
                GenerateLogString($"Описание полей не найдено в таблице '{table["displayName"]}'!");
                return null;
            }


            RdevTable rdevTable = new RdevTable();
            rdevTable.Name = table.Value<string>("name");
            rdevTable.DisplayName = table.Value<string>("displayName");

            foreach (JToken field in fields)
            {
                RdevField rdevField = new RdevField();

                JToken type = types.FirstOrDefault(x => x.Value<string>("name") == field.Value<string>("type"));

                rdevField.Name = field.Value<string>("name");
                rdevField.DisplayName = field.Value<string>("displayName");

                JToken relation = null;

                if (type != null)
                {
                    rdevField.Type = RdevType.GetType(type.Value<string>("type"));

                    if (rdevField.Type == null)
                    {
                        GenerateLogString($"Не удалось определить тип поля '{field["name"]}', таблицы '{table["name"]}'");
                        return null;
                    }

                    if (rdevField.Type == RdevType.RdevTypes.SysRelation)
                    {
                        relation = type["relation"];
                    }
                }
                else if (RdevType.GetType(field.Value<string>("type")) != null)
                {
                    rdevField.Type = RdevType.GetType(field.Value<string>("type"));

                    if (rdevField.Type == null)
                    {
                        GenerateLogString($"Не удалось определить тип поля '{field["name"]}', таблицы '{table["name"]}'");
                        return null;
                    }

                    if (rdevField.Type == RdevType.RdevTypes.SysRelation)
                    {
                        relation = field["relation"];
                    }
                }
                else
                {
                    relation = new JObject
                    {
                        { "table", "SysBaseTable" }
                    };
                }

                if (relation != null)
                {

                    JToken relatedTable = tables.FirstOrDefault(x => (x.Value<string>("name") ?? "").ToLower() == (relation.Value<string>("table") ?? "").ToLower());

                    if (relatedTable != null)
                    {
                        if (relatedTable.Value<string>("name") == table.Value<string>("name"))
                        {
                            continue;
                        }

                        var relatedTables = ProcessRdevTable(relatedTable, tables, types);
                        if (relatedTables != null)
                        {
                            foreach (var tab in relatedTables)
                            {
                                if (rdevTables.Find(x => (x.Name ?? "").ToLower() == (tab.Name ?? "").ToLower()) == null)
                                {
                                    rdevTables.Add(tab);
                                }
                                if (tab.Name == relation.Value<string>("table"))
                                {
                                    rdevField.RelatedTable = tab;
                                }
                            }
                        }
                        else
                        {
                            return null;
                        }

                    }
                    else
                    {
                        if (!exceptionTableNames.Contains(relation.Value<string>("table")))
                        {
                            GenerateLogString($"Не удалось найти таблицу '{relation.Value<string>("table")}', на которую указывает тип поля '{field["name"]}', таблицы '{table["name"]}'");
                            return null;
                        }
                        else
                        {
                            continue;
                        }
                        
                    }
                }
                rdevTable.AddField(rdevField);
            }

            rdevTables.Add(rdevTable);

            return rdevTables;
        }
    }
}
