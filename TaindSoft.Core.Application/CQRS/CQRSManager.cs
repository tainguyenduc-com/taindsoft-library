using TaindSoft.Core.Application.CQRS.Commands;
using TaindSoft.Core.Application.CQRS.Queries;

namespace TaindSoft.Core.Application.CQRS
{
    /// <summary>
    /// Facade for CQRS dispatcher
    /// Provides simplified API for sending commands and queries
    /// </summary>
    public interface ICQRSManager
    {
        /// <summary>
        /// Send a command/query and get result
        /// </summary>
        Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a command/query and get result
        /// </summary>
        Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a command without result
        /// </summary>
        Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of CQRS Manager using pure dispatcher
    /// </summary>
    public sealed class CQRSManager(ICQRSDispatcher dispatcher) : ICQRSManager
    {
        private readonly ICQRSDispatcher _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        public Task<TResult> SendAsync<TResult>(
            ICommand<TResult> command,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            return _dispatcher.DispatchAsync(command, cancellationToken);
        }

        public Task<TResult> SendAsync<TResult>(
            IQuery<TResult> query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            return _dispatcher.DispatchAsync(query, cancellationToken);
        }

        public Task SendAsync(
            ICommand command,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            return _dispatcher.DispatchAsync(command, cancellationToken);
        }
    }

    /// <summary>
    /// Extension methods for CQRS Manager
    /// </summary>
    public static class CQRSManagerExtensions
    {
        /// <summary>
        /// Send a command and get result
        /// </summary>
        public static Task<TResult> SendCommandAsync<TResult>(
            this ICQRSManager manager,
            ICommand<TResult> command,
            CancellationToken cancellationToken = default)
        {
            return manager.SendAsync(command, cancellationToken);
        }

        /// <summary>
        /// Send a command without result
        /// </summary>
        public static Task SendCommandAsync(
            this ICQRSManager manager,
            ICommand command,
            CancellationToken cancellationToken = default)
        {
            return manager.SendAsync(command, cancellationToken);
        }

        /// <summary>
        /// Send a query and get result
        /// </summary>
        public static Task<TResult> SendQueryAsync<TResult>(
            this ICQRSManager manager,
            IQuery<TResult> query,
            CancellationToken cancellationToken = default)
        {
            return manager.SendAsync(query, cancellationToken);
        }
    }
}
