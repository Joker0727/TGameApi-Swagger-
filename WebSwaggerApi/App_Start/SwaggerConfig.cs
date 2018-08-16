using System.Web.Http;
using WebActivatorEx;
using WebSwaggerApi;
using Swashbuckle.Application;
using WebSwaggerApi.Filter;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace WebSwaggerApi
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                    {
                        c.SingleApiVersion("v1", "WebSwaggerApi");
                        c.IncludeXmlComments(GetXmlCommentsPath());//读取WebSwaggerApi.XML
                        c.DescribeAllEnumsAsStrings();
                        c.OperationFilter<HttpHeaderFilter>();  // 权限过滤
                        c.OperationFilter<UploadFilter>();
                    })
                .EnableSwaggerUi(c =>
                    {
                        c.DocumentTitle("系统开发接口");
                        // 使用中文
                        c.InjectJavaScript(thisAssembly, "WebSwaggerApi.scripts.Swagger.Swagger_lang.js");
                    });
        }
        /// <summary>
        /// 读取WebSwaggerApi.XML
        /// </summary>
        /// <returns></returns>
        private static string GetXmlCommentsPath()
        {
            return string.Format("{0}/bin/WebSwaggerApi.XML", System.AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
