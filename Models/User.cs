using System;
using System.Collections.Generic;

namespace Ktpt.CourseOrder.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? StudentGroup { get; set; }

    public int RoleId { get; set; }

    public string? AdminPassword { get; set; }

    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();

    public virtual Role Role { get; set; } = null!;
}
