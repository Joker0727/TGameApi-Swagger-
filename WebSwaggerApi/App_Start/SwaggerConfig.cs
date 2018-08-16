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
                        c.IncludeXmlComments(GetXmlCommentsPath());//��ȡWebSwaggerApi.XML
                        c.DescribeAllEnumsAsStrings();
                        c.OperationFilter<HttpHeaderFilter>();  // Ȩ�޹���
                        c.OperationFilter<UploadFilter>();
                    })
                .EnableSwaggerUi(c =>
                    {
                        c.DocumentTitle("ϵͳ�����ӿ�");
                        // ʹ������
                        c.InjectJavaScript(thisAssembly, "WebSwaggerApi.scripts.Swagger.Swagger_lang.js");
                    });
        }
        /// <summary>
        /// ��ȡWebSwaggerApi.XML
        /// </summary>
        /// <returns></returns>
        private static string GetXmlCommentsPath()
        {
            return string.Format("{0}/bin/WebSwaggerApi.XML", System.AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
