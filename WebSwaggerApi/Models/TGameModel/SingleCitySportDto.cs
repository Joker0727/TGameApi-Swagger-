using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebSwaggerApi.Models.TGameModel
{
    public class SingleCitySportDto
    {
        public string Date { get; set; }

        public int HeavyCount { get; set; }
        public int CenterCount { get; set; }
        public int LowerCount { get; set; }
    }
}