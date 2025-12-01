using System;
using System.Collections.Generic;

namespace SMA.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string UserId { get; set; } = null!;

    public string CustomerId { get; set; } = null!;

    public decimal? TotalPrice { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? PointReceived { get; set; }

    public int? PointUsed { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual User User { get; set; } = null!;
}
