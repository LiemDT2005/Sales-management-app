using System;
using System.Collections.Generic;

namespace SMA.Models;

public partial class Customer
{
    public string CustomerId { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public int? Point { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
