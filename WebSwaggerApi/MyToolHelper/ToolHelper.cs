using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebSwaggerApi
{
    public class ToolHelper
    {
        /// <summary>
        /// 获取文件路径
        /// </summary>
        /// <param name="strPath"></param>
        /// <returns></returns>
        public string MapPath(string strPath)
        {
            if (HttpContext.Current != null)
            {
                return HttpContext.Current.Server.MapPath(strPath);
            }
            else //非web程序引用             
            {
                strPath = strPath.Replace("/", "\\");
                if (strPath.StartsWith("\\"))
                {
                    strPath = strPath.TrimStart('\\');
                }
                return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, strPath);
            }
        }
    }
}