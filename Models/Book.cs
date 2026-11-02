using System;
using System.Collections.Generic;

namespace Ktpt.CourseOrder.Models;

public partial class Book
{
    public int BookId { get; set; }

    public string Title { get; set; } = null!;

    public int PublisherId { get; set; }

    public int? PublishYear { get; set; }

    public string? Isbn { get; set; }

    public string? Genre { get; set; }

    public string? ImagePath { get; set; }

    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();

    public virtual ICollection<Copy> Copies { get; set; } = new List<Copy>();

    public virtual Publisher Publisher { get; set; } = null!;
}
