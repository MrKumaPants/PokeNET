// @name: Timeout Test - Infinite Loop
// @version: 1.0.0
// @description: Infinite loop to test timeout mechanism

while (true)
{
    // This should be cancelled by timeout
    System.Threading.Thread.Sleep(10);
}

return "Never reached";
