# FHIR Navigator
FHIR Navigator is framework for real world use of the FHIR API. It is a supporting abstraction layer around the basic FHIR interactions simplifying the most common use cases and challenges developers face when using these APIs in practice.

This package is an Non-Official support library for the amazing [Official Firely .NET SDK for HL7 FHIR](https://github.com/FirelyTeam/firely-net-sdk). This package take a hard dependency on the Hl7.Fhir.R4 NuGet package. 

What does FHIR Navigator provide:

- Automated OAuth2 Authentication token renewal support (ClientId, ClientSecret, Scopes, TokenEndPoint)
- Basic Authentication support (Username/Password) 
- Proxy Support
- Configure multiple named FHIR repositories, switch between each by name for FHIR API interactions
- Automated pagination support
- Resources returned via Reads or Searches are cache, allowing for simple access post query
- Automated resource reference resolution covering, Contained, Cached, and Repository        




