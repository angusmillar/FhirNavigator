namespace FhirNavigator.Exceptions;

public class FhirNavigatorException : ApplicationException
{
   public FhirNavigatorException(string? message) : base(message: message) {}
   public FhirNavigatorException(string? message, Exception? innerException) : base(message: message, innerException: innerException) { }
}