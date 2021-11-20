﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Hooking.Models
{
    public class HouseRules : BaseModel
    {
        [DisplayName("Kućni ljubimci")]
        public bool PetFriendly { get; set; }
        [DisplayName("Zabranjeno pušenje")]
        public bool NonSmoking { get; set; }
        [DisplayName("Vreme prijave u smeštaj")]
        public DateTime CheckinTime { get; set; }
        [DisplayName("Vreme odjave iz smeštaja")]
        public DateTime CheckoutTime { get; set; }
        [DisplayName("Limit za broj godina")]
        public int AgeRestriction { get; set; }
    }
}
