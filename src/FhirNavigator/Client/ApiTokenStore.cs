namespace FhirNavigator.Client;

public class ApiTokenStore : IApiTokenStore
{
  private Dictionary<string, ApiToken> TokenDictionary { get; set; }
    
  private Semaphore Semaphore { get; set; }
    
  public ApiTokenStore()
  {
    TokenDictionary = new();
    Semaphore = new Semaphore(initialCount: 1, maximumCount: 1);
  }


  /// <summary>
  /// True: If the token for the given key expire in the next 5min, or does not exist.
  /// False: If the token exists and is not expired. 
  /// </summary>
  /// <param name="key"></param>
  /// <returns></returns>
  public ApiToken? GetToken(string key)
  {
    if (TokenDictionary.ContainsKey(key))
    {
      return TokenDictionary[key];
    }
    return null;
  }

  /// <summary>
  /// Add a new Token or replace an existing token if already stored with the same key.
  /// </summary>
  /// <param name="key"></param>
  /// <param name="token"></param>
  public void AddOrReplaceToken(string key, ApiToken token)
  {
    //I have used a Semaphore here as I experienced the case where two RabbitMQ messages arrive concurrently
    //for the same order repository where no Token was stored in the TokenDictionary.
    //Both message threads then get a False on TokenDictionary.ContainsKey(key) and then both try and 
    //add the same keyed Token at the same time. Which throws an exception and message "An item with the same key has already been added. Key: GenieTest"
    Semaphore.WaitOne();
      
    if (TokenDictionary.ContainsKey(key))
    {
      TokenDictionary[key] = token;
    }
    else
    {
      TokenDictionary.Add(key, token);
    }

    Semaphore.Release();
  }

}