using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebSwaggerApi.Models
{
    public class User
    {
        public string Name { get; set; }
        public string Age { get; set; }
        public List<Hobbies> HobbyList { get; set; }
    }
    public class Hobbies
    {
        public string hName { get; set; }     
    }
}