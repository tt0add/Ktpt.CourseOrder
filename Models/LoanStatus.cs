using System;
using System.Collections.Generic;

namespace Ktpt.CourseOrder.Models;

public partial class LoanStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
