namespace FhirNavigator.Client;

/// <summary>
/// Jitter is a set of back off TimeSpans where each is of increasing time and yet does not have symmetry. So that is to say the amount of time to back off changes each time for each interval.
/// Its used so that you don't smash an external API when retrying. Let's say you have 10 threads all retrying, you don't want all 10 performing each retry at exactly the same time all in concert.
/// What you want is a Jitter where each is independently retrying at slightly different times intervals. They have Jitter!
/// </summary>
public interface IJitter
{
    public IEnumerable<TimeSpan> GetJitter(int maxRetries, TimeSpan seedDelay, TimeSpan maxDelay);
}