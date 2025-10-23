// @name: Security Test - Network Access
// @version: 1.0.0
// @description: Test script that attempts network access (should be blocked)

using System.Net.Http;

// This should throw a security exception
var client = new HttpClient();
var response = await client.GetStringAsync("http://example.com");

return response; // Should never reach here
