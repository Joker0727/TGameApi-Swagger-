﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using TGameApi.SqlServerDataAccess.EF.BFL;
using TGameApi.SqlServerDataAccess.EF.YWT;
using WebSwaggerApi.Models.TGameModel;
using WebSwaggerApi.MyToolHelper.TGameHelper;

namespace WebSwaggerApi.Controllers
{
    public class TGameController : ApiController
    {
        public YWTDbContext ywt = new YWTDbContext();
        public BFLDbContext bfl = new BFLDbContext();
        public TGameToolHelper gl = new TGameToolHelper();

        #region 出勤率、晨检
        /// <summary>
        /// 一个省的各个市，某段时间内所有园区的出勤率
        /// </summary>
        /// <param name="firstDateTime"></param>
        /// <param name="lastDateTime"></param>
        /// <param name="provincialName"></param>
        /// <returns></returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("cityAttendDetailAsync")]
        public async Task<string> CityAttendDetailAsync(
            string provincialName, DateTime firstDateTime,
            DateTime lastDateTime)
        {

            var attenFun = await ywt.bhyf_recordremark
               .Join(ywt.bhyf_student.Where(s => s.status == 1), r => (int)r.personid, s => s.personid, (r, s) => new { r.enterstatus, r.checktime, s.gardenid })
               .Join(ywt.bhyf_gardeninfo, x => x.gardenid, g => g.gid, (x, g) => new { g.city, x.checktime, x.enterstatus })
               .Join(ywt.bhyf_City, y => y.city, c => c.CityID, (y, c) => new { y.checktime, y.enterstatus, c.ProvinceID, c.CityName })
               .Join(ywt.bhyf_Province, z => z.ProvinceID, p => p.ProvinceID, (z, p) => new { z.CityName, z.enterstatus, z.checktime, p.ProvinceName })
               .Where(k => k.ProvinceName == provincialName && k.enterstatus == 1 && k.checktime >= firstDateTime && k.checktime < lastDateTime)
               .GroupBy(b => new { b.CityName, b.checktime.Year, b.checktime.Month, b.checktime.Day })
               .Select(m => new { cityName = m.Key.CityName, m.Key.Year, m.Key.Month, m.Key.Day, attenCount = m.Count() })
               .ToListAsync();


            var pCountFun = await ywt.bhyf_student
                .Join(ywt.bhyf_gardeninfo, s => s.gardenid, g => g.gid, (s, g) => new { g.city, sta = s.status, gta = g.status })
                .Join(ywt.bhyf_City, x => x.city, c => c.CityID, (x, c) => new { c.ProvinceID, c.CityName, x.sta, x.gta })
                .Join(ywt.bhyf_Province, y => y.ProvinceID, p => p.ProvinceID, (y, p) => new { y.CityName, p.ProvinceName, y.sta, y.gta })
                .Where(z => z.ProvinceName == provincialName && z.sta == 1 && z.gta == 1)
                .GroupBy(k => new { k.CityName })
                .Select(j => new { totalCount = j.Count(), cityName = j.Key.CityName, })
                .ToListAsync();
            var resList = attenFun
                .Join(pCountFun, a => a.cityName, p => p.cityName,
                (a, p) => new
                {
                    CityName = a.cityName,
                    CheckDate = new DateTime(a.Year, a.Month, a.Day).ToString("yyyy-MM-dd"),
                    AttenCount = a.attenCount,
                    TotalCount = p.totalCount,
                    Perenct = string.Format("{0:0.00%}", Convert.ToDouble(a.attenCount) / Convert.ToDouble(p.totalCount))
                });

            var selectList = attenFun.Select(a => new
            {
                CityName = a.cityName,
                CheckDate = new DateTime(a.Year, a.Month, a.Day).ToString("yyyy-MM-dd"),
                AttenCount = a.attenCount,
                TotalCount = pCountFun.FirstOrDefault(p => p.cityName == a.cityName).totalCount,
                Perenct = string.Format("{0:0.00%}", Convert.ToDouble(a.attenCount) / Convert.ToDouble(pCountFun.FirstOrDefault(p => p.cityName == a.cityName).totalCount))
            }).ToList();

            string json = JsonConvert.SerializeObject(selectList);
            return json;
        }
        #endregion

        #region 睡眠、运动

        #region  当日数据
        /// <summary>
        ///  一个省的各个市，当天所有园区的运动和睡眠
        /// </summary>
        /// <param name="provinceName">省名</param>
        /// <param name="cityName">市名</param>
        /// <param name="districtName">区/县名</param>
        /// <returns>返回一大串数据</returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("sportAndSleepAsync")]
        public async Task<object> SportAndSleepAsync(string provinceName = null, string cityName = null, string districtName = null)
        {
            string dtNowStr = DateTime.Now.ToString("yyy-MM-dd");
            var gardenList = await gl.GetGardenCityNameAndIDAsync(provinceName: provinceName, cityName: cityName, districtName: districtName);

            List<int> gidList = new List<int>();
            foreach (var g in gardenList)
            {
                gidList.Add(g.Gardenid);
            }

            var eList = await bfl.bhyf_bfl_move_analysis
                       .Where(a => gidList.Contains(a.gardenid.Value) &&
                       a.quality != null &&
                       a.type == 4
                     // && a.recordTime == dtNowStr
                     )
                        .GroupBy(g => new { g.gardenid, g.quality })
                        .Select(g => new
                        {
                            g.Key.gardenid,
                            g.Key.quality,
                            Count = g.Count(),
                            HeavySum = g.Sum(s => s.heavy),
                            CenterSum = g.Sum(s => s.center),
                            LowerSum = g.Sum(s => s.lower)
                        })
                .ToListAsync();

            var sportResult = gardenList
                .Join(eList, g => g.Gardenid, e => e.gardenid, (g, e) => new { g.Name, e })
                .GroupBy(b => new { b.Name })
                .Select(s => new
                {
                    Name = s.Key.Name,

                    AverageHeavy = double.Parse((Convert.ToDouble(s.Sum(x => x.e.HeavySum)) / Convert.ToDouble(s.Where(w => w.e.quality == "强度高").Sum(u => u.e.Count))).ToString("0.00")),
                    AverageCenter = double.Parse((Convert.ToDouble(s.Sum(x => x.e.CenterSum)) / Convert.ToDouble(s.Where(w => w.e.quality == "适中").Sum(u => u.e.Count))).ToString("0.00")),
                    AverageLower = double.Parse((Convert.ToDouble(s.Sum(x => x.e.LowerSum)) / Convert.ToDouble(s.Where(w => w.e.quality == "强度低").Sum(u => u.e.Count))).ToString("0.00")),

                    PercentHeavy = double.Parse((Convert.ToDouble(s.Sum(x => x.e.HeavySum)) / Convert.ToDouble(s.Sum(x => x.e.HeavySum + x.e.CenterSum + x.e.LowerSum))).ToString("0.00")),
                    PercentCenter = double.Parse((Convert.ToDouble(s.Sum(x => x.e.CenterSum)) / Convert.ToDouble(s.Sum(x => x.e.HeavySum + x.e.CenterSum + x.e.LowerSum))).ToString("0.00")),
                    PercentLowery = double.Parse((Convert.ToDouble(s.Sum(x => x.e.LowerSum)) / Convert.ToDouble(s.Sum(x => x.e.HeavySum + x.e.CenterSum + x.e.LowerSum))).ToString("0.00"))
                })
                .ToList();

            var sList = await bfl.bhyf_bfl_move_analysis
               .Where(a => gidList.Contains(a.gardenid.Value) &&
               a.quality != null &&
               a.type == 1 &&
               a.sleepTime != null &&
               a.sleepTime > 0
               //  && a.recordTime == dtNowStr
               )
               .GroupBy(g => new { g.gardenid, g.quality })
               .Select(g => new
               {
                   g.Key.gardenid,
                   g.Key.quality,
                   SleepCount = g.Count(),
                   SleepTime = g.Sum(m => m.sleepTime)
               })
               .ToListAsync();

            var sleepResult = gardenList
               .Join(sList, g => g.Gardenid, s => s.gardenid, (g, s) => new { g.Name, s.quality, s.SleepCount, s.SleepTime })
               .GroupBy(b => new { b.Name })
               .Select(m => new
               {
                   m.Key.Name,

                   SleepCount = m.Sum(n => n.SleepCount),
                   SleepTime = m.Sum(n => n.SleepTime),

                   AverageSleep = double.Parse((Convert.ToDouble(m.Sum(n => n.SleepTime)) / Convert.ToDouble(m.Sum(n => n.SleepCount))).ToString("0.00")),

                   AverageNoSleep = double.Parse((Convert.ToDouble(m.Where(w => w.quality == "基本没睡").Sum(u => u.SleepTime)) / Convert.ToDouble(m.Where(w => w.quality == "基本没睡").Sum(u => u.SleepCount))).ToString("0.00")),
                   AverageLessSleep = double.Parse((Convert.ToDouble(m.Where(w => w.quality == "睡眠少").Sum(u => u.SleepTime)) / Convert.ToDouble(m.Where(w => w.quality == "睡眠少").Sum(u => u.SleepCount))).ToString("0.00")),
                   AverageNormalSleep = double.Parse((Convert.ToDouble(m.Where(w => w.quality == "正常").Sum(u => u.SleepTime)) / Convert.ToDouble(m.Where(w => w.quality == "正常").Sum(u => u.SleepCount))).ToString("0.00")),
                   AverageMoreSleep = double.Parse((Convert.ToDouble(m.Where(w => w.quality == "睡眠多").Sum(u => u.SleepTime)) / Convert.ToDouble(m.Where(w => w.quality == "睡眠多").Sum(u => u.SleepCount))).ToString("0.00")),

                   PercentNoSleep = double.Parse((Convert.ToDouble(m.Where(w => w.quality == "基本没睡").Sum(u => u.SleepCount)) / Convert.ToDouble(m.Sum(u => u.SleepCount))).ToString("0.00")),
                   PercentLessSleep = double.Parse((Convert.ToDouble(m.Where(w => w.quality == "睡眠少").Sum(u => u.SleepCount)) / Convert.ToDouble(m.Sum(u => u.SleepCount))).ToString("0.00")),
                   PercentNormalSleep = double.Parse((Convert.ToDouble(m.Where(w => w.quality == "正常").Sum(u => u.SleepCount)) / Convert.ToDouble(m.Sum(u => u.SleepCount))).ToString("0.00")),
                   PercentMoreSleep = double.Parse((Convert.ToDouble(m.Where(w => w.quality == "睡眠多").Sum(u => u.SleepCount)) / Convert.ToDouble(m.Sum(u => u.SleepCount))).ToString("0.00"))
               })
               .ToList();

            Console.WriteLine(".................................综合信息.................................\r\n");

            var nameList = await gl.GetNameListAsync(provinceName: provinceName, cityName: cityName, districtName: districtName);

            List<PieceDemoSoprtAndSleepDto> pdsasdList = new List<PieceDemoSoprtAndSleepDto>();
            bool flag = false;
            foreach (var name in nameList)
            {
                PieceDemoSoprtAndSleepDto pdsasd = new PieceDemoSoprtAndSleepDto();

                foreach (var spr in sportResult)
                {
                    if (name == spr.Name)
                    {
                        pdsasd.AverageHeavy = spr.AverageHeavy;
                        pdsasd.AverageCenter = spr.AverageCenter;
                        pdsasd.AverageLower = spr.AverageLower;
                        pdsasd.PercentHeavy = spr.PercentHeavy;
                        pdsasd.PercentCenter = spr.PercentCenter;
                        pdsasd.PercentLowery = spr.PercentLowery;
                        flag = true;
                    }
                }
                foreach (var slr in sleepResult)
                {
                    if (name == slr.Name)
                    {
                        pdsasd.AverageSleep = slr.AverageSleep;
                        pdsasd.AverageNoSleep = slr.AverageNoSleep;
                        pdsasd.AverageLessSleep = slr.AverageLessSleep;
                        pdsasd.AverageNormalSleep = slr.AverageNormalSleep;
                        pdsasd.AverageMoreSleep = slr.AverageMoreSleep;
                        pdsasd.PercentNoSleep = slr.PercentNoSleep;
                        pdsasd.PercentLessSleep = slr.PercentLessSleep;
                        pdsasd.PercentNormalSleep = slr.PercentNormalSleep;
                        pdsasd.PercentMoreSleep = slr.PercentMoreSleep;
                        flag = true;
                    }
                }
                if (flag)
                {
                    pdsasd.Name = name;
                    pdsasdList.Add(pdsasd);
                    flag = false;
                }
            }
            string json = JsonConvert.SerializeObject(pdsasdList);
            object jobject = JsonConvert.DeserializeObject(json);
            return jobject;
        }

        /// <summary>
        /// 一个园所各个年级的平均睡眠时长
        /// </summary>
        /// <param name="gardenId">园所id</param>
        /// <returns></returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("gradeAverageSportAndSleepAsync")]
        public async Task<object> GradeAverageSportAndSleepAsync(int gardenId)
        {
            string dtNowStr = DateTime.Now.ToString("yyy-MM-dd");

            var sleep = await bfl.bhyf_bfl_move_analysis
                    .Where(w => w.gardenid == gardenId &&
                    w.type == 1 &&
                    w.quality != null &&
                    w.sleepTime > 0 &&
                    w.sleepTime != null
                    // && w.recordTime == dtNowStr
                    )
                    .GroupBy(g => new { g.gradeNum })
                    .Select(s => new
                    {
                        s.Key.gradeNum,
                        totalPerson = s.Count(),
                        totalSleepTime = s.Sum(m => m.sleepTime),
                    })
                    .ToListAsync();

            var sleepResult = sleep.Select(s => new GardenSleep
            {
                GradeNum = s.gradeNum,
                AverageSleepTime = double.Parse((Convert.ToDouble(s.totalSleepTime) / Convert.ToDouble(s.totalPerson)).ToString("0.00"))
            }).ToList();

            var sport = await bfl.bhyf_bfl_move_analysis
                .Where(w => w.gardenid == gardenId &&
                w.type == 4 &&
                w.quality != null
                // && w.recordTime == dtNowStr
                )
                .GroupBy(g => new { g.gradeNum })
                .Select(s => new
                {
                    s.Key.gradeNum,
                    totalPerson = s.Count(),
                    totalaveAmount = s.Sum(m => m.aveAmount)
                })
                .ToListAsync();

            var sportResult = sport.Select(s => new GardenSport
            {
                GradeNum = s.gradeNum,
                AverageSport = double.Parse((Convert.ToDouble(s.totalaveAmount) / Convert.ToDouble(s.totalPerson)).ToString("0.00"))
            }).ToList();

            GardenSportAndSleepDto gsasd = new GardenSportAndSleepDto
            {
                gardenSleep = sleepResult,
                gardenSport = sportResult
            };

            string json = JsonConvert.SerializeObject(gsasd);
            object jobject = JsonConvert.DeserializeObject(json);
            return jobject;
        }

        #endregion

        #region 对比数据

        #region 全省视角
        /// <summary>
        /// 省、市、区的平均睡眠时长和各种睡眠状态的人数 
        /// </summary>
        /// <param name="gardeNumber">年级id</param>
        /// <param name="firstDateTime">开始时间</param>
        /// <param name="lastDateTime">截止时间</param>
        /// <param name="provinceName">省名</param>
        /// <param name="cityName">市名</param>
        /// <param name="districtName">区/县名</param>
        /// <returns></returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("sleepCompareAsync")]
        public async Task<object> SleepCompareAsync(int gardeNumber,
            DateTime firstDateTime,
            DateTime lastDateTime,
            string provinceName = null,
            string cityName = null,
            string districtName = null)
        {
            string firstDateTimeStr = firstDateTime.ToString("yyy-MM-dd");
            string lastDateTimeStr = lastDateTime.ToString("yyy-MM-dd");

            var gardenList = await gl.GetGardenCityNameAndIDAsync(provinceName: provinceName, cityName: cityName, districtName: districtName);

            List<int> gidList = new List<int>();
            foreach (var g in gardenList)
            {
                gidList.Add(g.Gardenid);
            }

            var sList = await bfl.bhyf_bfl_move_analysis
                       .Where(a => gidList.Contains(a.gardenid.Value) &&
                       a.quality != null &&
                       a.type == 1 &&
                       a.sleepTime != null &&
                       a.sleepTime > 0 &&
                       a.gradeNum == gardeNumber &&
                       string.Compare(a.recordTime, firstDateTimeStr) > 0 &&
                       string.Compare(a.recordTime, lastDateTimeStr) < 0)
                        .GroupBy(g => new { g.gardenid })
                        .Select(g => new
                        {
                            g.Key.gardenid,
                            count = g.Count(),
                            SleepTimeTotal = g.Sum(s => s.sleepTime)
                        })
                .ToListAsync();

            var sleepTime = gardenList
                 .Join(sList, g => g.Gardenid, s => s.gardenid, (g, s) => new { g.Name, s.count, s.SleepTimeTotal })
                 .GroupBy(b => new { b.Name })
                 .Select(s => new
                 {
                     s.Key.Name,
                     TotalPerson = s.Sum(m => m.count),
                     SleepTimeTotal = s.Sum(m => m.SleepTimeTotal),
                     AverageSleepTime = s.Sum(m => m.SleepTimeTotal) / s.Sum(m => m.count)
                 }).ToList();

            var qList = await bfl.bhyf_bfl_move_analysis
                    .Where(a => gidList.Contains(a.gardenid.Value) &&
                    a.quality != null &&
                    a.type == 1 &&
                    a.gradeNum == gardeNumber &&
                    string.Compare(a.recordTime, firstDateTimeStr) > 0 &&
                    string.Compare(a.recordTime, lastDateTimeStr) < 0
                    )
                     .GroupBy(g => new { g.gardenid, g.quality })
                     .Select(g => new
                     {
                         g.Key.gardenid,
                         g.Key.quality,
                         count = g.Count()
                     })
             .ToListAsync();

            var sleepStatus = gardenList
             .Join(qList, g => g.Gardenid, s => s.gardenid, (g, s) => new { g.Name, s.quality, s.count })
             .GroupBy(b => new { b.Name })
              .Select(s => new
              {
                  s.Key.Name,
                  NoSleep = s.Where(w => w.quality == "基本没睡").Sum(k => k.count),
                  LessSleep = s.Where(w => w.quality == "睡眠少").Sum(k => k.count),
                  NormalSleep = s.Where(w => w.quality == "正常").Sum(k => k.count),
                  MoreSleep = s.Where(w => w.quality == "睡眠多").Sum(k => k.count)
              }).ToList();

            var resultList = sleepTime
                .Join(sleepStatus, st => st.Name, ss => ss.Name, (st, ss) => new
                {
                    st.Name,
                    st.AverageSleepTime,
                    ss.NoSleep,
                    ss.LessSleep,
                    ss.NormalSleep,
                    ss.MoreSleep
                })
                .Select(s => new
                {
                    s.Name,
                    s.AverageSleepTime,
                    s.NoSleep,
                    s.LessSleep,
                    s.NormalSleep,
                    s.MoreSleep,
                }).ToList();

            string json = JsonConvert.SerializeObject(resultList);
            object jobject = JsonConvert.DeserializeObject(json);
            return jobject;
        }
        /// <summary>
        /// 单个市、区某个时间段内，每个月份的各种睡眠状态的人数
        /// </summary>
        /// <param name="gardeNumber">年级id</param>
        /// <param name="firstDateTime">开始时间</param>
        /// <param name="lastDateTime">截止时间</param>
        /// <param name="cityName">市名</param>
        /// <param name="districtName">区/县名</param>
        /// <returns></returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("singleAreaSleepCompareAsync")]
        public async Task<object> SingleAreaSleepCompareAsync(int gardeNumber,
            DateTime firstDateTime,
            DateTime lastDateTime,
            string cityName = null,
            string districtName = null,
            int gardenId= -1)
        {
            string firstDateTimeStr = firstDateTime.ToString("yyy-MM-dd");
            string lastDateTimeStr = lastDateTime.ToString("yyy-MM-dd");
            List<int> gidList = new List<int>();
            List<GardenTemp> gardenList = new List<GardenTemp>();

            if (gardenId < 0)
            {
                gardenList = await gl.GetGardenCityNameAndIDAsync(cityName: cityName, districtName: districtName);

                foreach (var g in gardenList)
                {
                    gidList.Add(g.Gardenid);
                }
            }
            else
            {
                gidList.Add(gardenId);
            }

            var qList = await bfl.bhyf_bfl_move_analysis
                 .Where(a => gidList.Contains(a.gardenid.Value) &&
                 a.quality != null &&
                 a.type == 1 &&
                 a.gradeNum == gardeNumber &&
                 string.Compare(a.recordTime, firstDateTimeStr) > 0 &&
                 string.Compare(a.recordTime, lastDateTimeStr) < 0)
                  .GroupBy(g => new { g.quality, g.recordTime })
                  .Select(s => new
                  {
                      s.Key.quality,
                      s.Key.recordTime,
                      count = s.Count()
                  })
          .ToListAsync();

            List<SingleAreaCompareDto> singleList = new List<SingleAreaCompareDto>();
            foreach (var item in qList)
            {
                DateTime dt = Convert.ToDateTime(item.recordTime);
                singleList.Add(new SingleAreaCompareDto
                {
                    quality = item.quality,
                    recordTime = dt,
                    count = item.count
                });
            }
            var resultList = singleList
                .GroupBy(g => new
                {
                    g.recordTime.Year,
                    g.recordTime.Month
                })
            .Select(s => new
            {
                Date = s.Key.Year + "-" + (s.Key.Month > 9 ? s.Key.Month.ToString() : ("0" + s.Key.Month)) + "-01",
                NoSleep = s.Where(w => w.quality == "基本没睡").Sum(k => k.count),
                LessSleep = s.Where(w => w.quality == "睡眠少").Sum(k => k.count),
                NormalSleep = s.Where(w => w.quality == "正常").Sum(k => k.count),
                MoreSleep = s.Where(w => w.quality == "睡眠多").Sum(k => k.count)
            })
            .ToList();
            string json = JsonConvert.SerializeObject(resultList);
            object jobject = JsonConvert.DeserializeObject(json);
            return jobject;
        }
        /// <summary>
        ///  省、市、区的符合条件的三种运动强度的个数
        /// </summary>
        /// <param name="gardeNumber">年级id</param>
        /// <param name="firstDateTime">开始时间</param>
        /// <param name="lastDateTime">截止时间</param>
        /// <param name="provinceName">省名</param>
        /// <param name="cityName">市名</param>
        /// <param name="districtName">区/县名</param>
        /// <returns></returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("sportCompareAsync")]
        public async Task<object> SportCompareAsync(int gardeNumber,
            DateTime firstDateTime,
            DateTime lastDateTime,
            string provinceName = null,
            string cityName = null,
            string districtName = null)
        {
            string firstDateTimeStr = firstDateTime.ToString("yyy-MM-dd");
            string lastDateTimeStr = lastDateTime.ToString("yyy-MM-dd");

            var gardenList = await gl.GetGardenCityNameAndIDAsync(provinceName: provinceName, cityName: cityName, districtName: districtName);

            List<int> gidList = new List<int>();
            foreach (var g in gardenList)
            {
                gidList.Add(g.Gardenid);
            }

            var sList = await bfl.bhyf_bfl_move_analysis
                       .Where(a => gidList.Contains(a.gardenid.Value) &&
                       a.quality != null &&
                       a.type == 4 &&
                       a.gradeNum == gardeNumber &&
                       string.Compare(a.recordTime, firstDateTimeStr) > 0 &&
                       string.Compare(a.recordTime, lastDateTimeStr) < 0)
                        .GroupBy(g => new { g.gardenid })
                        .Select(g => new
                        {
                            g.Key.gardenid,
                            HeavySum = g.Sum(s => s.heavy),
                            CenterSum = g.Sum(s => s.center),
                            LowerSum = g.Sum(s => s.lower)
                        })
                .ToListAsync();

            var sportResult = gardenList
               .Join(sList, g => g.Gardenid, s => s.gardenid, (g, s) => new { g.Name, s.HeavySum, s.CenterSum, s.LowerSum })
               .GroupBy(b => new { b.Name })
               .Select(s => new
               {
                   s.Key.Name,
                   HeavySum = s.Sum(m => m.HeavySum),
                   CenterSum = s.Sum(m => m.CenterSum),
                   LowerSum = s.Sum(m => m.LowerSum)
               }).ToList();

            string json = JsonConvert.SerializeObject(sportResult);
            object jobject = JsonConvert.DeserializeObject(json);
            return jobject;
        }

        /// <summary>
        /// 市、区运动情况纵向对比
        /// </summary>
        /// <param name="gardeNumber">年级id</param>
        /// <param name="firstDateTime">开始时间</param>
        /// <param name="lastDateTime">截止时间</param>
        /// <param name="cityName">市名</param>
        /// <param name="districtName">区/县名92</param>
        /// <returns></returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("singleCitySportCompareAsync")]
        public async Task<object> SingleCitySportCompareAsync(int gardeNumber,
            DateTime firstDateTime,
            DateTime lastDateTime,
            string cityName = null,
            string districtName = null,
            int gardenId=-1)
        {
            string firstDateTimeStr = firstDateTime.ToString("yyy-MM-dd");
            string lastDateTimeStr = lastDateTime.ToString("yyy-MM-dd");
            List<int> gidList = new List<int>();
            List<GardenTemp> gardenList = new List<GardenTemp>();

            if (gardenId < 0)
            {
                gardenList = await gl.GetGardenCityNameAndIDAsync(cityName: cityName, districtName: districtName);

                foreach (var g in gardenList)
                {
                    gidList.Add(g.Gardenid);
                }
            }
            else
            {
                gidList.Add(gardenId);
            }

            var sList = await bfl.bhyf_bfl_move_analysis
                     .Where(a => gidList.Contains(a.gardenid.Value) &&
                     a.quality != null &&
                     a.type == 4 &&
                     a.gradeNum == gardeNumber &&
                     string.Compare(a.recordTime, firstDateTimeStr) > 0 &&
                     string.Compare(a.recordTime, lastDateTimeStr) < 0)
                      .GroupBy(g => new { g.recordTime, g.quality })
                      .Select(g => new
                      {
                          g.Key.recordTime,
                          g.Key.quality,
                          count = g.Count()
                      })
              .ToListAsync();

            List<SingleAreaCompareDto> singleList = new List<SingleAreaCompareDto>();
            foreach (var item in sList)
            {
                DateTime dt = Convert.ToDateTime(item.recordTime);
                singleList.Add(new SingleAreaCompareDto
                {
                    quality = item.quality,
                    recordTime = dt,
                    count = item.count
                });
            }

            var resultLiat = singleList.GroupBy(g => new
            {
                g.recordTime.Year,
                g.recordTime.Month
            })
            .Select(s => new
            {
                Date = s.Key.Year + "-" + (s.Key.Month > 9 ? s.Key.Month.ToString() : "0" + s.Key.Month) + "-01",
                HeavyCount = s.Where(w => w.quality == "强度高").Sum(m => m.count),
                CenterCount = s.Where(w => w.quality == "适中").Sum(m => m.count),
                LowerCount = s.Where(w => w.quality == "强度低").Sum(m => m.count)
            })
            .ToList();

            string json = JsonConvert.SerializeObject(resultLiat);
            object jobject = JsonConvert.DeserializeObject(json);
            return jobject;
        }
        #endregion

        #region 园所视角
        /// <summary>
        /// 当前园所一段时间内，不同年级的平均睡眠时长
        /// </summary>
        /// <param name="gardenId"></param>
        /// <param name="firstDateTime"></param>
        /// <param name="lastDateTime"></param>
        /// <returns></returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("gradeAverageSleep")]
        public async Task<object> GradeAverageSleepAsync(int gardenId,
            DateTime firstDateTime,
            DateTime lastDateTime)
        {
            string firstDateTimeStr = firstDateTime.ToString("yyy-MM-dd");
            string lastDateTimeStr = lastDateTime.ToString("yyy-MM-dd");

            var sleep = await bfl.bhyf_bfl_move_analysis
               .Where(w => w.gardenid == gardenId &&
                    w.type == 1 &&
                    w.quality != null &&
                    w.sleepTime > 0 &&
                    w.sleepTime != null &&
                    string.Compare(w.recordTime, firstDateTimeStr) > 0 &&
                    string.Compare(w.recordTime, lastDateTimeStr) < 0)
              .GroupBy(g => new { g.gradeNum })
                .Select(s => new
                {
                    s.Key.gradeNum,
                    totalPerson = s.Count(),
                    totalSleepTime = s.Sum(m => m.sleepTime)
                })
                .ToListAsync();

            var sleepResult = sleep.Select(s => new GardenSleep
            {
                GradeNum = s.gradeNum,
                AverageSleepTime = double.Parse((Convert.ToDouble(s.totalSleepTime) / Convert.ToDouble(s.totalPerson)).ToString("0.00"))
            }).ToList();

            string json = JsonConvert.SerializeObject(sleepResult);
            object jobject = JsonConvert.DeserializeObject(json);
            return jobject;
        }
        /// <summary>
        /// 当前园所一段时间内，某个睡眠状态下不同年纪的人数
        /// </summary>
        /// <param name="gardenId"></param>
        /// <param name="firstDateTime"></param>
        /// <param name="lastDateTime"></param>
        /// <param name="qualityType"></param>
        /// <returns></returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("gradeNumberSleep")]
        public async Task<object> GradeNumberSleepAsync(int gardenId,
            DateTime firstDateTime,
            DateTime lastDateTime,
            string qualityType)
        {
            string firstDateTimeStr = firstDateTime.ToString("yyy-MM-dd");
            string lastDateTimeStr = lastDateTime.ToString("yyy-MM-dd");

            var sleep = await bfl.bhyf_bfl_move_analysis
              .Where(w => w.gardenid == gardenId &&
                   w.type == 1 &&
                   w.sleepTime > 0 &&
                   w.sleepTime != null &&
                   string.Compare(w.recordTime, firstDateTimeStr) > 0 &&
                   string.Compare(w.recordTime, lastDateTimeStr) < 0 &&
                   w.quality == qualityType)
             .GroupBy(g => new { g.gradeNum })
               .Select(s => new
               {
                   GradeNum = s.Key.gradeNum,
                   Count = s.Count()
               })
               .ToListAsync();

            string json = JsonConvert.SerializeObject(sleep);
            object jobject = JsonConvert.DeserializeObject(json);
            return jobject;
        }
        /// <summary>
        /// 当前园所一段时间内，每月每种睡眠状态下的人数
        /// </summary>
        /// <param name="gardenId"></param>
        /// <param name="firstDateTime"></param>
        /// <param name="lastDateTime"></param>
        /// <param name="gradeNum"></param>
        [System.Web.Http.HttpGet, System.Web.Http.Route("gradeSleepTypeNumber")]
        public async Task<object> GradeSleepTypeNumberAsync(int gardenId,
            DateTime firstDateTime,
            DateTime lastDateTime,
            int gradeNum = -1)
        {
            string firstDateTimeStr = firstDateTime.ToString("yyy-MM-dd");
            string lastDateTimeStr = lastDateTime.ToString("yyy-MM-dd");

            List<SleepAndSportSearchDto> sleep = new List<SleepAndSportSearchDto>();
            if (gradeNum > 0)
            {
                sleep = await bfl.bhyf_bfl_move_analysis
                 .Where(w => w.gardenid == gardenId &&
                      w.type == 1 &&
                      w.sleepTime > 0 &&
                       w.quality != null &&
                      string.Compare(w.recordTime, firstDateTimeStr) > 0 &&
                      string.Compare(w.recordTime, lastDateTimeStr) < 0
                      && w.gradeNum == gradeNum
                      )
                .GroupBy(g => new { g.recordTime, g.quality })
                  .Select(s => new SleepAndSportSearchDto
                  {
                      recordTime = s.Key.recordTime,
                      quality = s.Key.quality,
                      count = s.Count()
                  }).ToListAsync();
            }
            else
            {
                sleep = await bfl.bhyf_bfl_move_analysis
                 .Where(w => w.gardenid == gardenId &&
                      w.type == 1 &&
                      w.sleepTime > 0 &&
                       w.quality != null &&
                      string.Compare(w.recordTime, firstDateTimeStr) > 0 &&
                      string.Compare(w.recordTime, lastDateTimeStr) < 0
                      )
                .GroupBy(g => new { g.recordTime, g.quality })
                  .Select(s => new SleepAndSportSearchDto
                  {
                      recordTime = s.Key.recordTime,
                      quality = s.Key.quality,
                      count = s.Count()
                  }).ToListAsync();
            }

            List<SingleAreaCompareDto> singleList = new List<SingleAreaCompareDto>();
            foreach (var item in sleep)
            {
                DateTime dt = Convert.ToDateTime(item.recordTime);
                singleList.Add(new SingleAreaCompareDto
                {
                    quality = item.quality,
                    recordTime = dt,
                    count = item.count
                });
            }
            var resultList = singleList
              .GroupBy(g => new
              {
                  g.recordTime.Year,
                  g.recordTime.Month
              })
          .Select(s => new SingleCitySleepDto
          {
              Date = s.Key.Year + "-" + (s.Key.Month > 9 ? s.Key.Month.ToString() : ("0" + s.Key.Month)) + "-01",
              NoSleep = s.Where(w => w.quality == "基本没睡").Sum(k => k.count),
              LessSleep = s.Where(w => w.quality == "睡眠少").Sum(k => k.count),
              NormalSleep = s.Where(w => w.quality == "正常").Sum(k => k.count),
              MoreSleep = s.Where(w => w.quality == "睡眠多").Sum(k => k.count)
          })
          .ToList();

            string json = JsonConvert.SerializeObject(resultList);
            object jobject = JsonConvert.DeserializeObject(json);
            return jobject;
        }
        /// <summary>
        /// 当前园所一段时间内，不同年纪的每种运动状态的人数
        /// </summary>
        /// <param name="gardenId"></param>
        /// <param name="firstDateTime"></param>
        /// <param name="lastDateTime"></param>
        /// <returns></returns>
        [System.Web.Http.HttpGet, System.Web.Http.Route("gradeNumberSport")]
        public async Task<object> GradeNumberSportAsync(int gardenId,
            DateTime firstDateTime,
            DateTime lastDateTime)
        {
            string firstDateTimeStr = firstDateTime.ToString("yyy-MM-dd");
            string lastDateTimeStr = lastDateTime.ToString("yyy-MM-dd");

            var sport = await bfl.bhyf_bfl_move_analysis
              .Where(w => w.gardenid == gardenId &&
                   w.type == 4 &&
                   w.quality != null &&
                   string.Compare(w.recordTime, firstDateTimeStr) > 0 &&
                   string.Compare(w.recordTime, lastDateTimeStr) < 0)
             .GroupBy(g => new { g.gradeNum, g.quality })
               .Select(s => new
               {
                   s.Key.gradeNum,
                   s.Key.quality,
                   count = s.Count()
               })
               .ToListAsync();
            var sportList = sport.GroupBy(g => new { g.gradeNum })
                .Select(s => new
                {
                    GradeNum = s.Key.gradeNum,
                    HeavyCount = s.Where(w => w.quality == "强度高").Sum(m => m.count),
                    LowerCount = s.Where(w => w.quality == "适中").Sum(m => m.count),
                    CenterCount = s.Where(w => w.quality == "强度低").Sum(m => m.count),
                }).ToList();

            string json = JsonConvert.SerializeObject(sportList);
            object jobject = JsonConvert.DeserializeObject(json);
            return jobject;
        }
        /// <summary>
        /// 当前园所一段时间内，每月每种运动状态下的人数
        /// </summary>
        /// <param name="gardenId"></param>
        /// <param name="firstDateTime"></param>
        /// <param name="lastDateTime"></param>
        /// <param name="gradeNum"></param>
        [System.Web.Http.HttpGet, System.Web.Http.Route("gradeSportTypeNumber")]
        public async Task<object> GradeSportTypeNumberAsync(int gardenId,
            DateTime firstDateTime,
            DateTime lastDateTime,
            int gradeNum = -1)
        {
            string firstDateTimeStr = firstDateTime.ToString("yyy-MM-dd");
            string lastDateTimeStr = lastDateTime.ToString("yyy-MM-dd");

            List<SleepAndSportSearchDto> sport = new List<SleepAndSportSearchDto>();
            if (gradeNum > 0)
            {
                sport = await bfl.bhyf_bfl_move_analysis
                 .Where(w => w.gardenid == gardenId &&
                      w.type == 4 &&
                       w.quality != null &&
                       w.gradeNum == gradeNum &&
                      string.Compare(w.recordTime, firstDateTimeStr) > 0 &&
                      string.Compare(w.recordTime, lastDateTimeStr) < 0
                      )
                .GroupBy(g => new { g.recordTime, g.quality })
                  .Select(s => new SleepAndSportSearchDto
                  {
                      recordTime = s.Key.recordTime,
                      quality = s.Key.quality,
                      count = s.Count()
                  }).ToListAsync();
            }
            else
            {
                sport = await bfl.bhyf_bfl_move_analysis
                  .Where(w => w.gardenid == gardenId &&
                       w.type == 4 &&
                        w.quality != null &&
                       string.Compare(w.recordTime, firstDateTimeStr) > 0 &&
                       string.Compare(w.recordTime, lastDateTimeStr) < 0
                       )
                 .GroupBy(g => new { g.recordTime, g.quality })
                   .Select(s => new SleepAndSportSearchDto
                   {
                       recordTime = s.Key.recordTime,
                       quality = s.Key.quality,
                       count = s.Count()
                   }).ToListAsync();
            }

            List<SingleAreaCompareDto> singleList = new List<SingleAreaCompareDto>();
            foreach (var item in sport)
            {
                DateTime dt = Convert.ToDateTime(item.recordTime);
                singleList.Add(new SingleAreaCompareDto
                {
                    quality = item.quality,
                    recordTime = dt,
                    count = item.count
                });
            }
            var resultLiat = singleList.GroupBy(g => new
            {
                g.recordTime.Year,
                g.recordTime.Month
            })
            .Select(s => new SingleCitySportDto
            {
                Date = s.Key.Year + "-" + (s.Key.Month > 9 ? s.Key.Month.ToString() : "0" + s.Key.Month) + "-01",
                HeavyCount = s.Where(w => w.quality == "强度高").Sum(m => m.count),
                CenterCount = s.Where(w => w.quality == "适中").Sum(m => m.count),
                LowerCount = s.Where(w => w.quality == "强度低").Sum(m => m.count)
            })
            .ToList();

            string json = JsonConvert.SerializeObject(resultLiat);
            object jobject = JsonConvert.DeserializeObject(json);
            return jobject;
        }

        #endregion

        #endregion

        #endregion
    }

}
