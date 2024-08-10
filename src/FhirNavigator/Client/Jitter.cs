namespace FhirNavigator.Client;

public class Jitter : IJitter
{
    // Adopting the 'Decorrelated Jitter' formula from https://www.awsarchitectureblog.com/2015/03/backoff.html.
    public IEnumerable<TimeSpan> GetJitter(int maxRetries, TimeSpan seedDelay, TimeSpan maxDelay)
    {
        Random jitterier = new Random();
        int attempt = 0;

        double seed = seedDelay.TotalMilliseconds;
        double max = maxDelay.TotalMilliseconds;
        double current = seed;

        while (attempt++ < maxRetries) 
        {
            // Can be between seed and previous * 3.  Mustn't exceed max.
            current = Math.Min(max, Math.Max(seed, current * 3 * jitterier.NextDouble())); 
            yield return TimeSpan.FromMilliseconds(current);
        }
    }
}