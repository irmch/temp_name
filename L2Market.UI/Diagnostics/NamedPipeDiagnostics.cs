using System.IO.Pipes;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace L2Market.UI.Diagnostics
{
    /// <summary>
    /// Diagnostic tool for NamedPipe connection testing
    /// </summary>
    public class NamedPipeDiagnostics
    {
        private readonly ILogger<NamedPipeDiagnostics> _logger;

        public NamedPipeDiagnostics(ILogger<NamedPipeDiagnostics> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Test if a NamedPipe server is running and accessible
        /// </summary>
        /// <param name="pipeName">Name of the pipe to test</param>
        /// <param name="timeoutMs">Connection timeout in milliseconds</param>
        /// <returns>Diagnostic result</returns>
        public async Task<NamedPipeDiagnosticResult> TestConnectionAsync(string pipeName, int timeoutMs = 5000)
        {
            var result = new NamedPipeDiagnosticResult
            {
                PipeName = pipeName,
                TestTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("Testing NamedPipe connection: {PipeName}", pipeName);

                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                
                var connectTask = client.ConnectAsync(timeoutMs);
                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask)
                {
                    await connectTask; // Get any exceptions
                    
                    if (client.IsConnected)
                    {
                        result.Success = true;
                        result.Message = "Connection successful";
                        _logger.LogInformation("NamedPipe connection test successful: {PipeName}", pipeName);

                        // Test sending a message
                        try
                        {
                            using var writer = new StreamWriter(client) { AutoFlush = true };
                            using var reader = new StreamReader(client);
                            
                            var testMessage = "{\"test\": \"connection\", \"timestamp\": \"" + DateTime.Now.ToString("O") + "\"}";
                            await writer.WriteLineAsync(testMessage);
                            
                            result.Message += " - Test message sent successfully";
                        }
                        catch (Exception ex)
                        {
                            result.Message += $" - Warning: Could not send test message: {ex.Message}";
                            _logger.LogWarning(ex, "Could not send test message to pipe {PipeName}", pipeName);
                        }
                    }
                    else
                    {
                        result.Success = false;
                        result.Message = "Connection failed - client not connected";
                        _logger.LogWarning("NamedPipe connection test failed - client not connected: {PipeName}", pipeName);
                    }
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Connection timeout after {timeoutMs}ms";
                    _logger.LogWarning("NamedPipe connection test timeout: {PipeName} (timeout: {Timeout}ms)", pipeName, timeoutMs);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Connection error: {ex.Message}";
                result.Exception = ex;
                _logger.LogError(ex, "NamedPipe connection test error: {PipeName}", pipeName);
            }

            return result;
        }

        /// <summary>
        /// List all available NamedPipes on the system
        /// </summary>
        /// <returns>List of pipe names</returns>
        public async Task<List<string>> ListAvailablePipesAsync()
        {
            var pipes = new List<string>();
            
            try
            {
                // Try to find pipes by testing common patterns
                var commonPatterns = new[]
                {
                    "l2market",
                    "l2",
                    "game",
                    "client"
                };

                foreach (var pattern in commonPatterns)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var pipeName = $"{pattern}{i}";
                        var result = await TestConnectionAsync(pipeName, 1000);
                        if (result.Success)
                        {
                            pipes.Add(pipeName);
                        }
                    }
                }

                // Also try to find pipes by PID pattern
                var processes = System.Diagnostics.Process.GetProcessesByName("l2");
                foreach (var process in processes)
                {
                    var pipeName = process.Id.ToString();
                    var result = await TestConnectionAsync(pipeName, 1000);
                    if (result.Success)
                    {
                        pipes.Add(pipeName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing available pipes");
            }

            return pipes;
        }
    }

    /// <summary>
    /// Result of NamedPipe diagnostic test
    /// </summary>
    public class NamedPipeDiagnosticResult
    {
        public string PipeName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime TestTime { get; set; }
        public Exception? Exception { get; set; }
    }
}
