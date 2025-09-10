namespace L2Market.Core.Services
{
    /// <summary>
    /// Service for parsing game packets from JSON data
    /// </summary>
    public interface IPacketParserService
    {
        /// <summary>
        /// Indicates if the service is currently running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Starts the packet parsing service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the packet parsing service
        /// </summary>
        void Stop();

        // ProcessPacketDataAsync removed - now handled via subscription
    }
}
