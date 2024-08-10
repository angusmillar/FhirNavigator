namespace FhirNavigator.Client
{
    public record ApiToken
    {
        public ApiToken(string value, string scheme, int expiresInSec, DateTime obtainedAt)
        {
            Value = value;
            Scheme = scheme;
            ExpiresInSec = expiresInSec;
            ObtainedAt = obtainedAt;
        }

        public string Value { get; }
        public string Scheme { get;}
        public int ExpiresInSec { get; }
        public DateTime ObtainedAt { get; }

        /// <summary>
        /// Returns Ture if the Token is due to expire in LessThan or EqualTo the ThresholdInMinutes value, which defaults to 5 mins
        /// </summary>
        /// <param name="thresholdInMinutes"></param>
        /// <returns></returns>
        public bool WillExpireSoon(int thresholdInMinutes = 5)
        {
            return (ObtainedAt.AddSeconds(ExpiresInSec).Subtract(DateTime.Now) <= TimeSpan.FromMinutes(thresholdInMinutes));
        }
    }
}