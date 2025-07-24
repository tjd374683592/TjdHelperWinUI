using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Tools
{
    public static class Adler32Helper
    {
        public static uint ComputeChecksum(string filePath)
        {
            const uint MOD_ADLER = 65521;
            uint a = 1, b = 0;

            using var stream = File.OpenRead(filePath);
            int read;
            byte[] buffer = new byte[4096];
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    a = (a + buffer[i]) % MOD_ADLER;
                    b = (b + a) % MOD_ADLER;
                }
            }

            return (b << 16) | a;
        }
    }
}
