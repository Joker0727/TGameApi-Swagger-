using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebSwaggerApi.Models.TGameModel
{
    public class SingleCitySleepDto
    {
        public string Date { get; set; }
        public int NoSleep { get; set; }
        public int LessSleep { get; set; }
        public int NormalSleep { get; set; }
        public int MoreSleep { get; set; }
    }
}