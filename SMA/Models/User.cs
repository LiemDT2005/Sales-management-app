using System;
using System.Collections.Generic;

namespace SMA.Models;

public partial class User
{
    public string UserId { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Avatar { get; set; }

    public DateTime? TokenExpiry { get; set; }

    public string Password { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public string Role { get; set; } = null!;

    public string? Status { get; set; }

    public string? PasswordRecoveryToken { get; set; }

    public string CitizenId { get; set; } = null!;

    public DateTime? HireDate { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
