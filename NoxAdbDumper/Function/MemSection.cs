using System;
using System.Text;
using System.Text.RegularExpressions;

namespace NoxAdbDumper
{
    public class MemSection
    {
        public UInt64 start;
        public UInt64 end;
        public string flags;
        public string desc;

        //private Regex memAddresses = new Regex("([a-fA-F0-9]+)-([a-fA-F0-9]+)", RegexOptions.Compiled);
        private Regex memSec = new Regex("([a-fA-F0-9]+)-([a-fA-F0-9]+)\\s(\\S+?)\\s([a-fA-F0-9]+)\\s\\S+?\\s\\d+\\s+(.*)", RegexOptions.Compiled);

        public MemSection(string info)
        {
            Match data = memSec.Match(info);
            if (!data.Success)
                throw new Exception("Невозможно определить адреса");
            start = Convert.ToUInt64(data.Groups[1].Value, 16);
            end = Convert.ToUInt64(data.Groups[2].Value, 16);
            flags = data.Groups[3].Value;
            desc = data.Groups[5].Value;

            //start = Convert.ToUInt32(info.Substring(0, 8), 16);
            //end = Convert.ToUInt32(info.Substring(9, 8), 16);
            //flags = info.Substring(18, 4);
            //if (info.Length > 49)
            //    desc = info.Substring(49);
            //else
            //    desc = "";
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}-{1} {2} {3} {4}", start.ToString("X8"), end.ToString("X8"), (end - start).ToString("X8"), flags, desc);
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            MemSection objAsPart = obj as MemSection;
            if (objAsPart == null) return false;
            else return Equals(objAsPart);
        }
        public override int GetHashCode()
        {
            return Convert.ToInt32(start);
        }
        public bool Equals(MemSection other)
        {
            if (other == null) return false;
            return (this.start.Equals(other.start));
        }
    }
}
