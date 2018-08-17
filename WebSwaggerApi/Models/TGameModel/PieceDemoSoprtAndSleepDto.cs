using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebSwaggerApi.Models.TGameModel
{
    public class PieceDemoSoprtAndSleepDto
    {
        /// <summary>
        /// 地区名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 运动强度高的平均运动时间
        /// </summary>
        public double AverageHeavy { get; set; }
        /// <summary>
        ///  运动强度中的平均运动时间
        /// </summary>
        public double AverageCenter { get; set; }
        /// <summary>
        ///  运动强度低的平均运动时间
        /// </summary>
        public double AverageLower { get; set; }
        /// <summary>
        /// 运动强度高的百分比
        /// </summary>
        public double PercentHeavy { get; set; }
        /// <summary>
        /// 运动强度中的百分比
        /// </summary>
        public double PercentCenter { get; set; }
        /// <summary>
        /// 运动强度低的百分比
        /// </summary>
        public double PercentLowery { get; set; }
        /// <summary>
        /// 平均睡眠时长
        /// </summary>
        public double AverageSleep { get; set; }
        /// <summary>
        /// 基本没睡的平均睡眠时长
        /// </summary>
        public double AverageNoSleep { get; set; }
        /// <summary>
        /// 睡眠较少的平均睡眠时长
        /// </summary>
        public double AverageLessSleep { get; set; }
        /// <summary>
        /// 睡眠正常的平均睡眠时长
        /// </summary>
        public double AverageNormalSleep { get; set; }
        /// <summary>
        /// 睡眠较多的平均睡眠时长
        /// </summary>
        public double AverageMoreSleep { get; set; }
        /// <summary>
        /// 基本没睡的百分比
        /// </summary>
        public double PercentNoSleep { get; set; }
        /// <summary>
        /// 睡眠较少的百分比
        /// </summary>
        public double PercentLessSleep { get; set; }
        /// <summary>
        /// 睡眠正常的百分比
        /// </summary>
        public double PercentNormalSleep { get; set; }
        /// <summary>
        /// 睡眠较多的百分比
        /// </summary>
        public double PercentMoreSleep { get; set; }
    }
}