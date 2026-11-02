using System;
using System.Collections.Generic;

namespace Ktpt.CourseOrder.Models;

public partial class Copy
{
    public int CopyId { get; set; }

    public int BookId { get; set; }

    public string? LocationShelf { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
