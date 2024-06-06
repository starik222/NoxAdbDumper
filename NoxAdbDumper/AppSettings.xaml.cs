using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NoxAdbDumper
{
    /// <summary>
    /// Логика взаимодействия для AppSettings.xaml
    /// </summary>
    public partial class AppSettings : Window
    {
        public AppSettings()
        {
            InitializeComponent();
            tb_Adb.Text = App.Settings.AdbPath;
            tb_DumpSaveDev.Text = App.Settings.DumpSavePathOnDevice;
            tb_DumpPathPC.Text = App.Settings.DumpSavePathOnPC;
        }

        private void btn_SelectAdb_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            tb_Adb.Text = openFileDialog.FileName;
        }

        private void btn_SelectDumpPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            if (openFolderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            tb_DumpPathPC.Text = openFolderDialog.Folder;
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.AdbPath = tb_Adb.Text;
            App.Settings.DumpSavePathOnDevice = tb_DumpSaveDev.Text;
            if (!App.Settings.DumpSavePathOnDevice.EndsWith("/"))
                App.Settings.DumpSavePathOnDevice += "/";
            App.Settings.DumpSavePathOnPC = tb_DumpPathPC.Text;
            App.Settings.SaveSettings();
            Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
