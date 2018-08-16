using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using WebSwaggerApi.Enums;
using WebSwaggerApi.Models;

namespace WebSwaggerApi.Controllers
{
    public class HomeController : ApiController
    {
        /// <summary>
        /// 测试一般类型的参数
        /// </summary>
        /// <param name="str">测试参数</param>
        /// <returns></returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("test")]
        public string Index(string str)
        {
            return str;
        }
        /// <summary>
        /// 测试传递对象参数
        /// </summary>
        /// <param name="user">对象</param>
        /// <returns></returns>
        [System.Web.Http.HttpPost, System.Web.Http.Route("object")]
        public string ObjectDemo(User user)
        {
            string hStr = string.Empty;
            foreach (Hobbies item in user.HobbyList)
            {
                hStr += item.hName + " ";
            }
            string str = "你好，我是" + user.Name + ",今年" + user.Age + "岁了,我的爱好是" + hStr;
            return str;
        }
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <returns></returns>
        [System.Web.Http.HttpPost, System.Web.Http.Route("user/upload")]
        public string UpLoadFile()
        {
            HttpFileCollection files = HttpContext.Current.Request.Files;
            ToolHelper tool = new ToolHelper();
            if (files.Count < 0)
            {
                return "上传文件为空！";
            }
            string path = tool.MapPath("/Data");
            for (int i = 0; i < files.Count; i++)
            {
                HttpPostedFile file = files[i];
                file.SaveAs(Path.Combine(path, file.FileName));
            }
            return "成功上传到" + path;
        }
        /// <summary>
        /// 枚举实现下拉框选择
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("choose/type")]
        public string ChooseType(TypeEnums type)
        {
            return "成功选择了" + type;
        }
    }
}