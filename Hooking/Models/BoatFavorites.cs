﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hooking.Models
{
    public class BoatFavorites : BaseModel
    {
        public string UserDetailsId { get; set; }
        public string BoatId { get; set; }
    }
}
