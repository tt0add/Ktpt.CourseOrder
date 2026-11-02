using System;
using System.Collections.Generic;

namespace Ktpt.CourseOrder.Models;

public partial class Loan
{
    public int LoanId { get; set; }

    public int CopyId { get; set; }

    public int UserId { get; set; }

    public int StatusId { get; set; }

    public DateTime LoanDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public virtual Copy Copy { get; set; } = null!;

    public virtual LoanStatus Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
