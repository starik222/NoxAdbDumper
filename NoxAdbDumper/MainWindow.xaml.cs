using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NoxAdbDumper
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists(App.SettingsPath))
            {
                App.Settings = JsonConvert.DeserializeObject<SettingsData>(File.ReadAllText(App.SettingsPath));
            }
            else
            {
                App.Settings = new SettingsData();
                App.Settings.AdbPath = Path.Combine(App.Root, "adb.exe");
                App.Settings.DumpSavePathOnDevice = "/data/data/com.wv.noxdumper/";
                App.Settings.DumpSavePathOnPC = Path.Combine(App.Root, "Dump");
                App.Settings.SaveSettings();
            }
        }

        private void MI_ConnectNox_Click(object sender, RoutedEventArgs e)
        {
            DeviceSelector d = new DeviceSelector();
            d.input = RunShell("netstat", "-aon", false);
            d.ShowDialog();
            if (d.output != "")
                MessageBox.Show(RunShell(App.Settings.AdbPath, "connect " + d.output));
        }

        private void MI_DisConnectNox_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(RunShell(App.Settings.AdbPath, "disconnect"));
        }

        private void MI_ProcessList_Click(object sender, RoutedEventArgs e)
        {
            App.procs = new List<ProcInfo>();
            //string[] commands = new string[] { "ps -t", "ps" };
            //string[] commands = new string[] { "ps -T" };
            string[] commands = new string[] { "ps" };
            foreach (string command in commands)
            {
                string result = RunNoxShell(command);
                int len = result.Length;
                while (true)
                {
                    result = result.Replace("  ", " ");
                    if (result.Length != len)
                        len = result.Length;
                    else
                        break;
                }
                StringReader sr = new StringReader(result);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!line.StartsWith("USER") && line.Trim() != "" && line.Contains(App.proc_filter))
                    {
                        try
                        {
                            App.procs.Add(new ProcInfo(line));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(line + "\n\n" + ex.Message);
                            //File.WriteAllText("error.log", line + "\n\n" + ex.Message);
                        }
                    }
                }
            }
            while (true)
            {
                bool found = false;
                for (int i = 0; i < App.procs.Count - 1; i++)
                {
                    if (App.procs[i + 1].pid < App.procs[i].pid)
                    {
                        found = true;
                        ProcInfo tmp = App.procs[i];
                        App.procs[i] = App.procs[i + 1];
                        App.procs[i + 1] = tmp;
                    }
                    if (App.procs[i + 1].pid == App.procs[i].pid)
                    {
                        App.procs.RemoveAt(i);
                        found = true;
                    }
                }
                if (!found)
                    break;
            }
            RefreshProcesses();
        }

        private async void MI_GetMemMap_Click(object sender, RoutedEventArgs e)
        {
            if (ls_Proc.SelectedIndex == -1) return;
            SetStatus("Please wait...");
            int n = (ls_Proc.SelectedItem as ProcStruct).Count;
            App.sections = new List<MemSection>();
            ProcInfo info = App.procs[n];
            menu1.IsEnabled = false;
            await Task.Run(() =>
            {
                GetMemoryMapAPI(info.pid);

                App.sections.Sort((x, y) => x.start.CompareTo(y.start));
            });
            RefreshSection();
            menu1.IsEnabled = true;
            SetStatus("Memory map received!");
        }

        private void MI_DumpSec_Click(object sender, RoutedEventArgs e)
        {
            if (ls_Proc.SelectedIndex == -1 || ls_Mem.SelectedIndex == -1) return;

            int n = (ls_Proc.SelectedItem as ProcStruct).Count;
            int m = Convert.ToInt32(ls_Mem.SelectedItem.ToString().Split('|')[0]);
            DumpSection(n, m, App.Settings.DumpSavePathOnPC);
        }

        private async void MI_DumpAllSec_Click(object sender, RoutedEventArgs e)
        {
            int num = 0;
            if (ls_Proc.SelectedIndex == -1) return;
            int n = (ls_Proc.SelectedItem as ProcStruct).Count;

            int m = ls_Mem.SelectedIndex;
            if (m != -1) num = Convert.ToInt32(ls_Mem.SelectedItem.ToString().Split('|')[0]);

            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.InitialFolder = App.Settings.DumpSavePathOnPC;
            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DumpFilter filter = new DumpFilter();
                if (!filter.ShowDialog().Value)
                {
                    filter.Close();
                    return;
                }
                List<string> flagsFilter = filter.GetExcludedFlags();
                List<string> sectNameFilter = filter.GetSectionNameFilter();
                bool isSectNameFilter = filter.cb_sectContains.IsChecked.Value;
                filter.Close();
                pb1.Maximum = App.sections.Count;
                menu1.IsEnabled = false;
                await Task.Run(() =>
                {
                    for (int i = num; i < App.sections.Count; i++)
                    {
                        bool dumpSection = true;

                        if (flagsFilter.Any(a => App.sections[i].flags.StartsWith(a)))
                        {
                            dumpSection = false;
                        }
                        else if (isSectNameFilter && !sectNameFilter.Any(a => App.sections[i].desc.Contains(a)))
                        {
                            dumpSection = false;
                        }

                        if (dumpSection)
                        {
                            try
                            {
                                DumpSection(n, i, openFolderDialog.Folder);
                            }
                            catch (Exception ex)
                            {
                                string name = "sec" + i.ToString("D4") + "_" + CleanName(App.sections[i].desc);
                                Dispatcher.Invoke(() =>
                                {
                                    tb_log.AppendText("Error for section #" + i + " '" + name + "'!\n" + ex.Message + "\r\n");
                                });
                            }
                        }

                        Dispatcher.Invoke(() =>
                        {
                            pb1.Value = i;
                            st_Text.Content = $"Progress: {i - num} / {App.sections.Count - num}";
                        });
                    }

                });
                menu1.IsEnabled = true;
                pb1.Value = 0;
                
            }
        }

        private async void MI_DumpAllSecOnDev_Click(object sender, RoutedEventArgs e)
        {
            int num = 0;
            if (ls_Proc.SelectedIndex == -1) return;
            int n = (ls_Proc.SelectedItem as ProcStruct).Count;

            int m = ls_Mem.SelectedIndex;
            if (m != -1) num = Convert.ToInt32(ls_Mem.SelectedItem.ToString().Split('|')[0]);
            DumpFilter filter = new DumpFilter();
            if (!filter.ShowDialog().Value)
            {
                filter.Close();
                return;
            }
            List<string> flagsFilter = filter.GetExcludedFlags();
            List<string> sectNameFilter = filter.GetSectionNameFilter();
            bool isSectNameFilter = filter.cb_sectContains.IsChecked.Value;
            filter.Close();
            pb1.Maximum = App.sections.Count;
            menu1.IsEnabled = false;
            await Task.Run(() =>
            {
                for (int i = num; i < App.sections.Count; i++)
                {
                    bool dumpSection = true;

                    if (flagsFilter.Any(a => App.sections[i].flags.StartsWith(a)))
                    {
                        dumpSection = false;
                    }
                    else if (isSectNameFilter && !sectNameFilter.Any(a => App.sections[i].desc.Contains(a)))
                    {
                        dumpSection = false;
                    }

                    if (dumpSection)
                    {
                        string name = "sec" + i.ToString("D4") + "_" + CleanName(App.sections[i].desc);
                        try
                        {
                            DumpSectionOnDevice(n, i);
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                tb_log.AppendText("Error for section #" + i + " '" + name + "'!\n" + ex.Message + "\r\n");
                            });
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        pb1.Value = i;
                        SetStatus($"Progress: {i - num} / {App.sections.Count - num}");
                    });
                }

            });
            menu1.IsEnabled = true;
            pb1.Value = 0;
        }

        private void SetStatus(string text)
        {
            Dispatcher.Invoke(() =>
            {
                st_Text.Content = text;
            });
        }

        private void MI_DumpAllMem_Click(object sender, RoutedEventArgs e)
        {
            if (ls_Proc.SelectedIndex == -1) return;
            int n = (ls_Proc.SelectedItem as ProcStruct).Count;
            ProcInfo info = App.procs[n];

            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.InitialFolder = App.Settings.DumpSavePathOnDevice;
            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RunShell(App.Settings.AdbPath, "push pmdump /data/local/tmp/pmdump");
                RunNoxShell("chmod 755 /data/local/tmp/pmdump");
                RunNoxShell("cd /data/local/tmp && ./pmdump +r +w -x +p " + info.pid);
                RunShell(App.Settings.AdbPath, "pull data/local/tmp/output_pmdump.bin " + $"\"{Path.Combine(App.Settings.DumpSavePathOnPC, "dump.bin")}\"");
                RunNoxShell("rm /data/local/tmp/output_pmdump.bin");
            }
        }

        private void MI_Kill9_Click(object sender, RoutedEventArgs e)
        {
            if (ls_Proc.SelectedIndex == -1) return;
            int n = (ls_Proc.SelectedItem as ProcStruct).Count;

            ProcInfo info = App.procs[n];
            RunNoxShell("kill -9 " + info.pid);
        }

        private void MI_Kill19_Click(object sender, RoutedEventArgs e)
        {
            if (ls_Proc.SelectedIndex == -1) return;
            int n = (ls_Proc.SelectedItem as ProcStruct).Count;

            ProcInfo info = App.procs[n];
            RunNoxShell("kill -19 " + info.pid);
        }

        private void tb_Proc_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshProcesses();

            List<ProcStruct> tmpList = new List<ProcStruct>();
            foreach (ProcStruct s in ls_Proc.Items)
            {
                if (s.Description.Contains(tb_Proc.Text))
                    tmpList.Add(s);
            }

            ls_Proc.ItemsSource = tmpList;
        }

        private void tb_Mem_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshSection();

            List<string> tmpList = new List<string>();
            foreach (string s in ls_Mem.Items)
            {
                if (s.Contains(tb_Mem.Text))
                    tmpList.Add(s);
            }

            ls_Mem.Items.Clear();

            foreach (string s in tmpList)
            {
                ls_Mem.Items.Add(s);
            }
        }

        public string RunShell(string command, string args, bool toLog = true)
        {
            Process process = new Process();
            StringBuilder outputStringBuilder = new StringBuilder();
            try
            {
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = args;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.EnableRaisingEvents = false;
                process.OutputDataReceived += (sender, eventArgs) => outputStringBuilder.AppendLine(eventArgs.Data);
                process.ErrorDataReceived += (sender, eventArgs) => outputStringBuilder.AppendLine(eventArgs.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                var processExited = process.WaitForExit(10000);
                var output = outputStringBuilder.ToString();
                if (processExited == false)
                    process.Kill();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                process.Close();
            }
            string result = outputStringBuilder.ToString();
            if (toLog)
            {
                Dispatcher.Invoke(() =>
                {
                    //tb_log.Text = String.Empty;
                    tb_log.AppendText(">" + command + " " + args + "\r\n");
                    tb_log.AppendText(result.Replace("\r\n\r\n", "\r\n"));
                });
            }
            return result;
        }

        public string RunNoxShell(string command)
        {
            bool rootMode = false;
            Dispatcher.Invoke(() => rootMode = MI_SuMode.IsChecked);
            string cmdPrefix = rootMode ? "shell su -c " : "shell ";
            return RunShell(App.Settings.AdbPath, cmdPrefix + command);
        }

        public void GetMemoryMapAPI(uint pid)
        {
            string result = RunNoxShell("cat /proc/" + pid + "/maps");
            StringReader sr = new StringReader(result);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Trim() != "" && line.Contains(App.sect_filter))
                {
                    MemSection sec = new MemSection(line);
                    App.sections.Add(sec);
                }
            }
        }

        private void DumpSection(int p, int s, string outDir)
        {
            ProcInfo info = App.procs[p];
            MemSection section = App.sections[s];
            UInt64 size = section.end - section.start;
            string dumpName = string.Format("dump_{0}_{1}-{2}.bin", s, section.start.ToString("X"), section.end.ToString("X"));

            Directory.CreateDirectory(outDir);
            RunNoxShell($"mkdir {App.Settings.DumpSavePathOnDevice}");
            RunNoxShell("dd if=/proc/" + info.pid + $"/mem of={App.Settings.DumpSavePathOnDevice}dump bs=1 count=" + size + " skip=" + section.start);
            RunShell(App.Settings.AdbPath, "pull /data/data/com.wv.noxdumper/dump \"" + Path.Combine(outDir, dumpName) + "\"");
        }

        private void DumpSectionOnDevice(int p, int s)
        {
            ProcInfo info = App.procs[p];
            MemSection section = App.sections[s];
            UInt64 size = section.end - section.start;
            string dumpName = string.Format("dump_{0}_{1}-{2}.bin", s, section.start.ToString("X"), section.end.ToString("X"));

            RunNoxShell($"mkdir {App.Settings.DumpSavePathOnDevice}");
            RunNoxShell("dd if=/proc/" + info.pid + $"/mem of={App.Settings.DumpSavePathOnDevice}{dumpName} bs=1 count=" + size + " skip=" + section.start);
        }

        public void RefreshProcesses()
        {
            int count = 0;

            List<ProcStruct> LRC = new List<ProcStruct>();

            foreach (ProcInfo info in App.procs)
            {
                LRC.Add(new ProcStruct { Count = count++, PID = info.pid, PPID = info.ppid, Description = String.Format("{0} {1} ({2})", info.pc, info.name, info.user) });
            }

            ls_Proc.ItemsSource = LRC;
        }

        public void RefreshSection()
        {
            int count = 0;
            ls_Mem.Items.Clear();
            foreach (MemSection section in App.sections)
            {
                if (section.ToString().Contains(App.sect_filter))
                {
                    ls_Mem.Items.Add($"{count.ToString().PadLeft(5, '0')}| {section.ToString()}");
                    count++;
                }

            }
        }

        public string CleanName(string name)
        {
            return name.Replace("\\", "_")
                       .Replace("/", "_")
                       .Replace("<", "_")
                       .Replace(">", "_")
                       .Replace("?", "_")
                       .Replace("@", "_")
                       .Replace(":", "_")
                       .Replace("*", "_")
                       .Replace("\"", "_")
                       .Replace(":", "_")
                       .Replace("|", "_");
        }

        public class ProcStruct
        {
            public int Count { get; set; }
            public uint PID { get; set; }

            public uint PPID { get; set; }

            public string Description { get; set; }
        }

        private void Mi_Options_Click(object sender, RoutedEventArgs e)
        {
            AppSettings settings = new AppSettings();
            settings.ShowDialog();
        }
    }
}
