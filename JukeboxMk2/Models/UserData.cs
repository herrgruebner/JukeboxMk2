using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JukeboxMk2.Models
{
    public class UserData
    {
        public string Name { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
