// @name: Performance Test - Intensive Loop
// @version: 1.0.0
// @description: CPU-intensive calculation for performance testing

// Calculate first N fibonacci numbers
int n = 30;
var fib = new long[n];
fib[0] = 0;
fib[1] = 1;

for (int i = 2; i < n; i++)
{
    fib[i] = fib[i - 1] + fib[i - 2];
}

return fib[n - 1];
