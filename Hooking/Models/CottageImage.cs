﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hooking.Models
{
    public class CottageImage 
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; }
        public string CottageId { get; set; }
    }
}
