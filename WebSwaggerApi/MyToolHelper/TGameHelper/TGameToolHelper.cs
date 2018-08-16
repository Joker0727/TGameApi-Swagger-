using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TGameApi.SqlServerDataAccess.EF.BFL;
using TGameApi.SqlServerDataAccess.EF.YWT;
using WebSwaggerApi.Models.TGameModel;

namespace WebSwaggerApi.MyToolHelper.TGameHelper
{
    public class TGameToolHelper
    {
        public YWTDbContext ywt = new YWTDbContext();
        public BFLDbContext bfl = new BFLDbContext();

        /// <summary>
        /// 返回符合条件的园区名称和ID
        /// </summary>
        /// <param name="provinceName"></param>
        /// <param name="cityName"></param>
        /// <param name="districtName"></param>
        /// <returns></returns>
        public async Task<List<GardenTemp>> GetGardenCityNameAndIDAsync(string provinceName = null, string cityName = null, string districtName = null)
        {
            List<GardenTemp> gardenList = new List<GardenTemp>();

            if (provinceName != null)
            {

                gardenList = await ywt.bhyf_gardeninfo
                 .Join(ywt.bhyf_City, g => g.city, c => c.CityID, (g, c) => new { c.ProvinceID, c.CityID, c.CityName, g.gid, g.gname })
                 .Join(ywt.bhyf_Province, x => x.ProvinceID, p => p.ProvinceID, (x, p) => new { p.ProvinceID, p.ProvinceName, x.CityID, x.CityName, x.gid, x.gname })
                 .Where(y => y.ProvinceName == provinceName)
                 .GroupBy(n => new { n.CityName, n.gid })
                 .Select(m => new GardenTemp
                 {
                     Name = m.Key.CityName,
                     Gardenid = m.Key.gid
                 })
                 .ToListAsync();
            }
            if (cityName != null)
            {
                gardenList = await ywt.bhyf_gardeninfo
                  .Join(ywt.bhyf_District, g => g.district, d => d.DistrictID, (g, d) => new { d.CityID, d.DistrictID, d.DistrictName, g.gid, g.gname })
                  .Join(ywt.bhyf_City, x => x.CityID, c => c.CityID, (x, c) => new { c.CityID, c.CityName, x.DistrictID, x.DistrictName, x.gid, x.gname })
                  .Where(y => y.CityName == cityName)
                  .GroupBy(n => new { n.DistrictName, n.gid })
                  .Select(m => new GardenTemp
                  {
                      Name = m.Key.DistrictName,
                      Gardenid = m.Key.gid
                  })
                  .ToListAsync();
            }
            if (districtName != null)
            {
                gardenList = await ywt.bhyf_gardeninfo
                  .Join(ywt.bhyf_District, g => g.district, d => d.DistrictID, (g, d) => new { d.DistrictID, d.DistrictName, g.gid, g.gname })
                  .Where(y => y.DistrictName == districtName)
                  .GroupBy(n => new { n.gid, n.gname })
                  .Select(m => new GardenTemp
                  {
                      Name = m.Key.gname,
                      Gardenid = m.Key.gid
                  })
                  .ToListAsync();
            }
            return gardenList;
        }
        /// <summary>
        /// 返回符合条件的地区名称
        /// </summary>
        /// <param name="provinceName"></param>
        /// <param name="cityName"></param>
        /// <param name="districtName"></param>
        /// <returns></returns>
        public async Task<List<string>> GetNameListAsync(string provinceName = null, string cityName = null, string districtName = null)
        {
            List<string> nameList = new List<string>();

            if (provinceName != null)
            {
                nameList = await ywt.bhyf_City
                      .Join(ywt.bhyf_Province, c => c.ProvinceID, p => p.ProvinceID, (c, p) => new { p.ProvinceName, c.CityName })
                      .Where(y => y.ProvinceName == provinceName)
                      .Select(s => s.CityName)
                      .ToListAsync();
            }
            if (cityName != null)
            {
                nameList = await ywt.bhyf_District
                        .Join(ywt.bhyf_City, c => c.CityID, p => p.CityID, (c, p) => new { p.CityName, c.DistrictName })
                        .Where(y => y.CityName == cityName)
                        .Select(s => s.DistrictName)
                        .ToListAsync();
            }
            if (districtName != null)
            {
                nameList = await ywt.bhyf_gardeninfo
                    .Join(ywt.bhyf_District, c => c.district, p => p.DistrictID, (c, p) => new { p.DistrictName, c.gname })
                    .Where(y => y.DistrictName == districtName)
                    .Select(s => s.gname)
                    .ToListAsync();
            }
            return nameList;
        }
    }
}