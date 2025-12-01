using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMA.Models
{
    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public int PointReceived { get; set; }
        public int PointUsed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

