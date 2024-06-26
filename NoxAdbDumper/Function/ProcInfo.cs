﻿using System;
using System.Text;

namespace NoxAdbDumper
{
    public class ProcInfo
    {
        public string user;
        public uint pid;
        public uint ppid;
        public uint vsize;
        public uint rss;
        public uint wchan;
        public string pc;
        public string flag;
        public string name;
        public ProcInfo(string info)
        {
            string[] parts = info.Split(' ');
            user = parts[0];
            pid = Convert.ToUInt32(parts[1]);
            ppid = Convert.ToUInt32(parts[2]);
            vsize = Convert.ToUInt32(parts[3]);
            rss = Convert.ToUInt32(parts[4]);

            if (parts.Length == 9)
            {
                wchan = Convert.ToUInt32(parts[5], 16);
                pc = parts[6];
                flag = parts[7];
                name = parts[8];
            }
            else if (parts.Length == 8)
            {
                pc = parts[5];
                flag = parts[6];
                name = parts[7];
            }
            else
                throw new NotImplementedException("Unknown 'shell ps' result");
        }
    }
}
