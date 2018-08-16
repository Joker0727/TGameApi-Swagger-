using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebSwaggerApi.Models.TGameModel
{
    public class SingleAreaCompareDto
    {
        public string quality { get; set; }
        public DateTime recordTime { get; set; }
        public int count { get; set; }
    }
}