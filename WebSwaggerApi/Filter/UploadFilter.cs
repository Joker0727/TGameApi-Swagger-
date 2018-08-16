using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Description;

namespace WebSwaggerApi.Filter
{
    public class UploadFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            if (apiDescription.RelativePath.Contains("upload"))
            {
                operation.consumes.Add("application/form-data");
                operation.parameters.Add(new Parameter
                {
                    name = "document",
                    @in = "formData",
                    description = "文件上传",
                    required = true,
                    type = "file",

                });
            }
        }
    }
}