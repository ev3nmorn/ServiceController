using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace Service_Control
{
    public partial class MainForm : Form
    {
        private string pathToFile;
        private OpenFileDialog formOpenFileDialog;
        private SaveFileDialog formSaveFileDialog;

        private void CreateOpenFileDialog()
        {
            formOpenFileDialog = new OpenFileDialog();
            formOpenFileDialog.FileName = String.Empty;
            formOpenFileDialog.DefaultExt = AppConstants.ConfigExtension;
            formOpenFileDialog.Filter = AppConstants.Filter;
        }

        private void CreateSaveFileDialog()
        {
            formSaveFileDialog = new SaveFileDialog();
            formSaveFileDialog.OverwritePrompt = true;
            formSaveFileDialog.FileName = AppConstants.DefaultFileName;
            formSaveFileDialog.DefaultExt = AppConstants.ConfigExtension;
            formSaveFileDialog.Filter = AppConstants.Filter;
        }

        private void RefreshDefaultFileNameOFD()
        {
            formOpenFileDialog.FileName = String.Empty;
        }

        private void RefreshDefaultFileNameSFD()
        {
            formSaveFileDialog.FileName = AppConstants.DefaultFileName;
        }

        private void WriteServices(List<string> services)
        {          
            StreamWriter serviceStreamWriter = new StreamWriter(pathToFile, false, Encoding.Default);

            serviceStreamWriter.WriteLine(AppConstants.InstructionsLine1);
            serviceStreamWriter.WriteLine(AppConstants.InstructionsLine2);

            foreach (string service in services)
                serviceStreamWriter.WriteLine(service);

            serviceStreamWriter.Close();

            statusWriteService.AppendText(AppConstants.ServicesWereWritten
                + pathToFile + Environment.NewLine + AppConstants.BlockEnd + Environment.NewLine);
        }  

        private List<string> ReadConfigFile()
        {
            string temp;
            List<string> result = new List<string>();
            StreamReader servicesStreamReader = new StreamReader(pathToFile, Encoding.Default);

            while ((temp = servicesStreamReader.ReadLine()) != null)
                result.Add(temp);

            ServiceControlFunctions.DeleteComments(ref result);

            servicesStreamReader.Close();
            return result;
        }  

        private void DisplayStatus(List<KeyValuePair<int, string>> status, ref int counter)
        {
            foreach (KeyValuePair<int, string> element in status)
                if (element.Key == 1)
                {
                    statusWriteService.AppendText(element.Value + Environment.NewLine + Environment.NewLine);
                    ++counter;
                }
        }

        private void SubmitConfigs()
        {
            int counter = 0;
            List<string> configStrings = ReadConfigFile();
            List<KeyValuePair<int, string>> status = new List<KeyValuePair<int, string>>();

            foreach (string configLine in configStrings)
            {
                status = ServiceControlFunctions.ApplySettings(configLine);
                DisplayStatus(status, ref counter);
            }

            if (counter != 0)
            {
                List<string> lines = statusWriteService.Lines.ToList();
                lines.RemoveAt(lines.Count - 1);
                statusWriteService.Lines = lines.ToArray();
            }
            else
                statusWriteService.AppendText(AppConstants.NoAnyChanges + Environment.NewLine);

            statusWriteService.AppendText(AppConstants.BlockEnd + Environment.NewLine);
        }

        public MainForm()
        {
            InitializeComponent();
            CreateOpenFileDialog();
            CreateSaveFileDialog();
            pathToFile = String.Empty;         
        }

        private void WriteButton_Click(object sender, EventArgs e)
        {
            if (pathToFile.Length == 0)
            {
                MessageBox.Show(AppConstants.NoFileSelected);
                return;
            }
            WriteServices(ServiceControlFunctions.GetAllServices());
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            if (pathToFile.Length == 0)
            {
                MessageBox.Show(AppConstants.NoFileSelected);
                return;
            }

            SubmitConfigs();
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            RefreshDefaultFileNameSFD();
            if (formSaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                pathToFile = formSaveFileDialog.FileName;
                pathTextBox.Text = pathToFile;
                File.Create(pathToFile).Close();
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            RefreshDefaultFileNameOFD();
            if (formOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                pathToFile = formOpenFileDialog.FileName;
                pathTextBox.Text = pathToFile;
            }
        }
    }
}
