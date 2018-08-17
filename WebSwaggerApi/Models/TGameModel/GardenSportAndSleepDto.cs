using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebSwaggerApi.Models.TGameModel
{
    public class GardenSportAndSleepDto
    {
        public List<GardenSleep> gardenSleep { get; set; }
        public List<GardenSport> gardenSport { get; set; }
    }

    public class GardenSleep
    {
        public int? GradeNum { get; set; }
        public double AverageSleepTime { get; set; }
    }
    public class GardenSport
    {
        public int? GradeNum { get; set; }
        public double AverageSport { get; set; }
    }
}