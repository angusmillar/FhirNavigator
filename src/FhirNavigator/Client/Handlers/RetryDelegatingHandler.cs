using Microsoft.Extensions.Logging;
using System.Net;

namespace FhirNavigator.Client.Handlers;

public class RetryDelegatingHandler(ILogger<RetryDelegatingHandler> logger, IJitter jitter) : DelegatingHandler
{
    private const int MaxRetries = 10;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(2);
    private TimeSpan[]? _retryIntervals;
    private int _attempt;

    private static readonly HttpStatusCode[] RetryableHttpStatusCodeList =
    [
        HttpStatusCode.GatewayTimeout,
        HttpStatusCode.RequestTimeout,
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.ServiceUnavailable
    ];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        HttpResponseMessage? response = null;
        for (_attempt = 0; _attempt < MaxRetries; _attempt++)
        {
            lastException = null;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
                if (!IsRetryableHttpStatusCode(response))
                {
                    return response;
                }

                await RetryDelayAndLog(cancellationToken, response);
            }
            catch (Exception exception) when (exception is HttpRequestException or TimeoutException)
            {
                await ExceptionDelayAndLog(cancellationToken, exception);
                lastException = exception;
            }
        }

        if (lastException is not null)
        {
            throw lastException;
        }

        ArgumentNullException.ThrowIfNull(response);

        return response;
    }

    private async Task ExceptionDelayAndLog(CancellationToken cancellationToken, Exception exception)
    {
        LogException(exception);
        if (IsLastAttempt())
        {
            return;
        }
        
        await Task.Delay(GetRetryDelay(), cancellationToken);
    }

    private static bool IsRetryableHttpStatusCode(HttpResponseMessage response)
    {
        return RetryableHttpStatusCodeList.Contains(response.StatusCode);
    }

    private bool IsLastAttempt()
    {
        return (_attempt + 1 >= MaxRetries);
    }
    private async Task RetryDelayAndLog(CancellationToken cancellationToken,
        HttpResponseMessage response)
    {
        if (IsLastAttempt())
        {
            LogRetryDelay(response.StatusCode);
            return;
        }
        
        int? retrySeconds = HasRetrySeconds(response);
        if (retrySeconds is null)
        {
            LogRetryDelay(response.StatusCode);
            await Task.Delay(GetRetryDelay(), cancellationToken);
            return;
        }

        TimeSpan retryTimeSpan = TimeSpan.FromSeconds(Convert.ToDouble(retrySeconds));
        LogRetryDelay(response.StatusCode, retryTimeSpan);
        await Task.Delay(retryTimeSpan, cancellationToken);
    }

    private int? HasRetrySeconds(HttpResponseMessage response)
    {
        const string headerKeyName = "Retry-After";
        if (response.Headers.TryGetValues(headerKeyName, out IEnumerable<string>? headerValueList))
        {
            if (int.TryParse(headerValueList.First(), out int retrySeconds))
            {
                return retrySeconds;
            }
        }

        return null;
    }

    private TimeSpan GetRetryDelay()
    {
        if (_retryIntervals is null)
        {
            _retryIntervals = jitter.GetJitter(MaxRetries, InitialDelay, MaxDelay).ToArray();
        }

        return _retryIntervals[_attempt];
    }

    private void LogException(Exception lastException)
    {
        logger.LogWarning(lastException, "Request attempt {Attempt} of {MaxRetries}: ", _attempt + 1, MaxRetries);
    }

    private void LogRetryDelay(HttpStatusCode httpStatusCode, TimeSpan? retryTimeSpan = null)
    {
        if (retryTimeSpan is null)
        {
            logger.LogWarning(
                "Request attempt {Attempt} of {MaxRetries}: Retryable Http Status Code of {HttpStatusCodeNumber} {HttpStatusCodeName} returned. ",
                _attempt + 1, MaxRetries, (int)httpStatusCode, httpStatusCode);
            return;
        }

        logger.LogWarning(
            "Request attempt {Attempt} of {MaxRetries}: Retryable Http Status Code of {HttpStatusCodeNumber} {HttpStatusCodeName} returned. Instructed to wait {RetryAfterSeconds} seconds by 'Retry-After' response header. ",
            _attempt + 1, MaxRetries, (int)httpStatusCode, httpStatusCode, retryTimeSpan);
    }
}