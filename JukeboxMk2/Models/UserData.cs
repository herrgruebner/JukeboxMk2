﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JukeboxMk2.Models
{
    public class UserData
    {
        public string JukeBoxId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string PlaylistId { get; set; }
    }
}
