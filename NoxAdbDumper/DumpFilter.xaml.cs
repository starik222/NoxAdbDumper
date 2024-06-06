using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NoxAdbDumper
{
    /// <summary>
    /// Логика взаимодействия для DumpFilter.xaml
    /// </summary>
    public partial class DumpFilter : Window
    {
        public DumpFilter()
        {
            InitializeComponent();
        }

        public List<string> GetExcludedFlags()
        {
            List<string> flags = new List<string>();
            if (cb_Read.IsChecked.Value)
                flags.Add("r-");
            if (cb_ReadWrite.IsChecked.Value)
                flags.Add("rw-");
            if (cb_Rwx.IsChecked.Value)
                flags.Add("rwx");
            if (cb_Priv.IsChecked.Value)
                flags.Add("---p");
            return flags;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btn_addContains_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(tb_addSectContains.Text))
            {
                lb_SectContains.Items.Add(tb_addSectContains.Text);
            }
        }

        public List<string> GetSectionNameFilter()
        {
            List<string> result = new List<string>();
            for (int i = 0; i < lb_SectContains.Items.Count; i++)
            {
                result.Add((string)lb_SectContains.Items[i]);
            }
            return result;
        }
    }
}
