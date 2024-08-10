using Microsoft.Extensions.Logging;
using Sonic.Fhir.Tools.Placer.Application.Infrastructure;

namespace FhirNavigator.Client;

public abstract class HttpClientBase
{
  private readonly IHttpClientFactory HttpClientFactory;
  protected readonly ILogger<HttpClientBase> Logger;
    
  protected delegate HttpRequestMessage HttpRequestMessageFactory();

  protected HttpClientBase(IHttpClientFactory httpClientFactory, ILogger<HttpClientBase> logger)
  {     
    HttpClientFactory = httpClientFactory;
    Logger = logger;
  }

  protected async Task<Result<HttpResponseMessage>> RetryEnabledSendAsync(string httpClientName, HttpRequestMessageFactory HttpRequestMessageFactory)
  {
    Result<HttpResponseMessage> Result = await SendAsync(httpClientName, HttpRequestMessageFactory.Invoke());

    if (Result.Success)
    {
      return Result;
    }
    int RetryCount = 1;
    int MaxRetries = 4;            
    while (Result.Retryable && (RetryCount <= MaxRetries))
    {
      TimeSpan RetryInterval = RetryCount switch
      {
        1 => TimeSpan.FromMilliseconds(250),  //Wait 250 ms
        2 => TimeSpan.FromMilliseconds(1000), //Wait 250 ms
        3 => TimeSpan.FromMilliseconds(3000), //Wait 500 ms          
        _ => TimeSpan.FromMilliseconds(5000), //Wait 5 sec
      };

      Thread.Sleep(RetryInterval);
      Result = await SendAsync(httpClientName, HttpRequestMessageFactory.Invoke());               
      RetryCount++;
    }
    if (Result.Retryable)
    {
      return Result<HttpResponseMessage>.Fail($"Failed to send to the {httpClientName} after {MaxRetries} retries attempts, last error message was: {Result.ErrorMessage}");
    }
    else
    {
      return Result;
    }
  }


  private async Task<Result<HttpResponseMessage>> SendAsync(string httpClientName, HttpRequestMessage HttpRequestMessage)
  {
    try
    {
      using HttpClient Client = HttpClientFactory.CreateClient(httpClientName);
      return Result<HttpResponseMessage>.Ok(await Client.SendAsync(HttpRequestMessage));             
    }
    catch (HttpRequestException HttpRequestException)
    {
      string Message = $"HttpRequestException when calling the API: {HttpRequestException.Message}";
      Logger.LogWarning(HttpRequestException, Message);
      return Result<HttpResponseMessage>.Retry(Message);
    }
    catch (TimeoutException TimeoutException)
    {
      string Message = $"TimeoutException when calling the API: {TimeoutException.Message}";
      Logger.LogWarning(TimeoutException, Message);
      return Result<HttpResponseMessage>.Retry(Message);
    }
    catch (OperationCanceledException OperationCanceledException)
    {
      string Message = $"OperationCanceledException when calling the API: {OperationCanceledException.Message}";
      Logger.LogWarning(OperationCanceledException, Message);
      return Result<HttpResponseMessage>.Fail(Message);
    }

  }
}