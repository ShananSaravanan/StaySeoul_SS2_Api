using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace StaySeoul_SS2Api.Models
{
    public class PropertyPrice
    {
        public string title { get; set; }
        public long ID { get; set; }
        public string iconName { get; set; }
        public string color { get;set; }
        public decimal price { get; set; }
        public string rules { get; set; }
        public DateTime date { get; set; }
        public string dispRules => "("+rules+")";
    }
}