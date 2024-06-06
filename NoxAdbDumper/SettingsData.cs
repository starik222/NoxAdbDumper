using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoxAdbDumper
{
    public class SettingsData
    {
        public string AdbPath { get; set; }
        public string DumpSavePathOnDevice { get; set; }
        public string DumpSavePathOnPC { get; set; }

        public void SaveSettings()
        {
            File.WriteAllText(App.SettingsPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
