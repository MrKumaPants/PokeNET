// @name: Security Test - File Access
// @version: 1.0.0
// @description: Test script that attempts file system access (should be blocked)

using System.IO;

// This should throw a security exception
File.WriteAllText("/tmp/malicious.txt", "This should not work");

return "File written"; // Should never reach here
