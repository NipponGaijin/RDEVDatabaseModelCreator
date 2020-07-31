using Newtonsoft.Json.Linq;
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
        string _outputFolderPath;
        string _openedFolder;

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
                fileDialog.Filter = "Проектные файлы JSON (*.json)|usr.json";

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
                            GenerateLogString($"Файл успешно разобран.");
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

            foreach (JToken table in tables)
            {
                JToken fields = table["fields"];
                if(fields == null)
                {
                    GenerateLogString($"Описание полей не найдено в таблице '{table["displayName"]}'!");
                    return;
                }
            }
        }
    }
}
