using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Tools
{
    public class SHAHelper
    {
        public static string PasswordEncryption(string pwd)
        {
            //创建SHA1加密算法对象
            SHA1 sha1 = SHA1.Create();
            //将原始密码转换为字节数组
            byte[] originalPwd = Encoding.UTF8.GetBytes(pwd);
            //执行加密
            byte[] encryPwd = sha1.ComputeHash(originalPwd);
            //将加密后的字节数组转换为字符串
            return string.Join("", encryPwd.Select(b => string.Format("{0:x2}",
          b)).ToArray()).ToLower();
        }
    }
}
