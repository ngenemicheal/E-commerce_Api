namespace ECommerce.Application.Exceptions;

public class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key,
                failureGroup => failureGroup.ToArray());
    }

    public ValidationException(IDictionary<string, string[]> errors) : this()
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}
