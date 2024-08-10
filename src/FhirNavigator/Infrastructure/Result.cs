namespace Sonic.Fhir.Tools.Placer.Application.Infrastructure;

public partial class Result
{
  protected Result(bool success, bool retryable, string errorMessage)
  {
    this.Success = success;
    this.ErrorMessage = errorMessage;
    this.Retryable = retryable;
  }

  public static string ErrorMessagesDelimiter = ", ";

  /// <summary>
  /// The action was Successful and the result value property will be set. 
  /// </summary>
  public bool Success { get; private set; }
  /// <summary>
  /// Indicates that the operation failed and no Result value can be returned. Refer the ErrorMessage for more detail.   
  /// </summary>
  public bool Failure => !Success;
  /// <summary>
  /// Indicate that this result believes the operation could be retried again. 
  /// It is a decision of the caller to check Retryable or not, checking Success or Failure is usualy enough.
  /// /// If Retryable is True then Success SHALL BE False and Failure SHALL BE True 
  /// If Success is True then Retryable will be False
  /// </summary>    
  public bool Retryable { get; private set; }
  /// <summary>
  /// An error message that is populated when Success is False and Failure is true, is empty string of Success
  /// </summary>
  public string ErrorMessage { get; private set; }

  public static Result Ok()
  {
    return new Result(success: true, retryable: false, errorMessage: string.Empty);
  }

  public static Result Retry(string message)
  {
    return new Result(success: false, retryable: true, errorMessage: message);
  }

  public static Result Fail(string message)
  {
    return new Result(success: false, retryable: false, errorMessage: message);
  }

  public static Result Combine(params Result[] resultArray)
    => Combine(resultArray, ErrorMessagesDelimiter);
  public static Result Combine(string errorMessageDelimiter, params Result[] resultArray)
    => Combine(resultArray, errorMessageDelimiter);

  public static Result Combine<T>(params Result<T>[] results)
    => Combine(results, ErrorMessagesDelimiter);
  public static Result Combine<T>(string errorMessageDelimiter, params Result<T>[] resultArray)
    => Combine(resultArray, errorMessageDelimiter);

  public static Result Combine(IEnumerable<Result> resultList, string? errorMessageDelimiter = null)
  {
    List<Result> failedResultList = resultList.Where(x => x.Failure).ToList();

    if (failedResultList.Count == 0)
      return Result.Ok();

    string combinedMessages = string.Join(errorMessageDelimiter ?? ErrorMessagesDelimiter, AggregateMessages(failedResultList.Select(x => x.ErrorMessage)));

    return Result.Fail(combinedMessages);
  }

  public static Result Combine<T>(IEnumerable<Result<T>> typedResultList, string? errorMessageDelimiter = null)
  {
    IEnumerable<Result> untypedResultList = typedResultList.Select(result => (Result)result);
    return Combine(untypedResultList, errorMessageDelimiter);
  }

  private static IEnumerable<string> AggregateMessages(IEnumerable<string> messages)
  {
    var dict = new Dictionary<string, int>();
    foreach (var message in messages)
    {
      if (!dict.ContainsKey(message))
        dict.Add(message, 0);

      dict[message]++;
    }
    return dict.Select(x => x.Value == 1 ? x.Key : $"{x.Key} ({x.Value}×)");
  }

}

public class Result<T> : Result
{
  private T? ResultValue { get; set; }
  public T Value
  {
    get
    {
      if (Success)
      {
        if (Nullable.GetUnderlyingType(typeof(T)) != null)
        {
          //It is a Nullable type (e.g DateTime?) therefore returning null is allowed even though the return value type is only T and not T?.
          //Because in this case T represents T? (e.g T is DateTime? and not DateTime??) 
#pragma warning disable CS8603 // Possible null reference return.
          return ResultValue;
#pragma warning restore CS8603 // Possible null reference return.
        }

        if (ResultValue is object)
        {
          return ResultValue;
        }

        throw new ArgumentNullException("Value is null and the type in use is not a Nullable type.");
      }

      throw new ApplicationException("Call to get Result.value when the result was not successfully.");
    }
  }
  
  public static Result<T> Ok(T value)
  {
    return new Result<T>(value: value, success: true, errorMessage: string.Empty);
  }

  public static new Result<T> Retry(string message)
  {
    return new Result<T>(success: false, retryable: true, errorMessage: message);
  }

  public static new Result<T> Fail(string message)
  {
    return new Result<T>(success: false, retryable: false, errorMessage: message);
  }


  protected internal Result(T value, bool success, string errorMessage)
    : base(success: success, retryable: false, errorMessage: errorMessage)
  {
    this.ResultValue = value;
  }

  protected internal Result(bool success, bool retryable, string errorMessage)
    : base(success: success, retryable: retryable, errorMessage: errorMessage)
  {
    this.ResultValue = default;
  }

}