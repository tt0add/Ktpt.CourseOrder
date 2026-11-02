using System;

namespace Ktpt.CourseOrder.Services;

public class AppException : Exception
{
    public AppException(string message) : base(message) { }
}