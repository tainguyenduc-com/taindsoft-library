using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TaindSoft.Core.Application.CQRS.Commands;
using TaindSoft.Core.Application.CQRS.CQRSPipelines;
using TaindSoft.Core.Application.CQRS.Queries;

namespace TaindSoft.Core.Application.CQRS
{
    /// <summary>
    /// Pure CQRS dispatcher without MediatR
    /// Handles command and query dispatching with pipeline support
    /// </summary>
    public interface ICQRSDispatcher
    {
        Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
        Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
        Task DispatchAsync(ICommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of CQRS dispatcher with compile-time safe generic pipeline execution
    /// </summary>
    public sealed class CQRSDispatcher(IServiceProvider serviceProvider) : ICQRSDispatcher
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        public Task<TResult> DispatchAsync<TResult>(
            ICommand<TResult> command,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            return DispatchCoreAsync<TResult>(command, typeof(ICommandHandler<,>), cancellationToken);
        }

        public Task<TResult> DispatchAsync<TResult>(
            IQuery<TResult> query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            return DispatchCoreAsync<TResult>(query, typeof(IQueryHandler<,>), cancellationToken);
        }

        public Task DispatchAsync(
            ICommand command,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            return DispatchCommandAsync(command, cancellationToken);
        }

        private Task<TResult> DispatchCoreAsync<TResult>(
            object request,
            Type handlerTypeDefinition,
            CancellationToken cancellationToken)
        {
            Type requestType = request.GetType();
            Type resultType = typeof(TResult);

            // Use reflection once to bridge to strongly-typed generic method
            MethodInfo dispatchMethod = typeof(CQRSDispatcher)
                .GetMethod(nameof(DispatchWithPipelines), BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Method {nameof(DispatchWithPipelines)} not found");

            MethodInfo genericMethod = dispatchMethod.MakeGenericMethod(requestType, resultType);
            Task<TResult> task = (Task<TResult>)genericMethod.Invoke(this, [request, handlerTypeDefinition, cancellationToken])!;

            return task;
        }

        private async Task<TResult> DispatchWithPipelines<TRequest, TResult>(
            TRequest request,
            Type handlerTypeDefinition,
            CancellationToken cancellationToken)
            where TRequest : notnull
        {
            // Resolve handler with full generic type information
            Type handlerInterfaceType = handlerTypeDefinition.MakeGenericType(typeof(TRequest), typeof(TResult));
            object handler = _serviceProvider.GetService(handlerInterfaceType)
                ?? throw new InvalidOperationException($"No handler registered for {typeof(TRequest).Name}");

            // Resolve all pipeline behaviors with concrete types - fully compile-time safe
            List<ICQRSPipelineBehavior<TRequest, TResult>> behaviors = [.. _serviceProvider
                .GetServices<ICQRSPipelineBehavior<TRequest, TResult>>()
                .Reverse()];

            // Build strongly-typed handler function
            Func<Task<TResult>> handlerFunc = async () =>
            {
                MethodInfo handleMethod = handlerInterfaceType.GetMethod("Handle")
                    ?? throw new InvalidOperationException("Handler does not have Handle method");
                Task<TResult> task = (Task<TResult>)handleMethod.Invoke(handler, [request, cancellationToken])!;
                return await task;
            };

            // Wrap handler with each behavior - all types known at compile time
            foreach (ICQRSPipelineBehavior<TRequest, TResult> behavior in behaviors)
            {
                Func<Task<TResult>> currentFunc = handlerFunc;
                // Fully compile-time safe: behavior is ICQRSPipelineBehavior<TRequest, TResult>
                // No dynamic, no runtime type resolution required
                handlerFunc = () => behavior.Handle(request, currentFunc, cancellationToken);
            }

            return await handlerFunc();
        }

        private Task DispatchCommandAsync(
            object request,
            CancellationToken cancellationToken)
        {
            Type requestType = request.GetType();

            // Use reflection once to bridge to strongly-typed generic method
            MethodInfo dispatchMethod = typeof(CQRSDispatcher)
                .GetMethod(nameof(DispatchCommandWithPipelines), BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Method {nameof(DispatchCommandWithPipelines)} not found");

            MethodInfo genericMethod = dispatchMethod.MakeGenericMethod(requestType);
            Task task = (Task)genericMethod.Invoke(this, [request, cancellationToken])!;

            return task;
        }

        private async Task DispatchCommandWithPipelines<TRequest>(
            TRequest request,
            CancellationToken cancellationToken)
            where TRequest : notnull
        {
            // Resolve handler with full generic type information
            Type handlerInterfaceType = typeof(ICommandHandler<>).MakeGenericType(typeof(TRequest));
            object handler = _serviceProvider.GetService(handlerInterfaceType)
                ?? throw new InvalidOperationException($"No handler registered for {typeof(TRequest).Name}");

            // Resolve all pipeline behaviors - using Unit as result type for commands without result
            List<ICQRSPipelineBehavior<TRequest, Unit>> behaviors = [.. _serviceProvider
                .GetServices<ICQRSPipelineBehavior<TRequest, Unit>>()
                .Reverse()];

            // Build strongly-typed handler function
            Func<Task<Unit>> handlerFunc = async () =>
            {
                MethodInfo handleMethod = handlerInterfaceType.GetMethod("Handle")
                    ?? throw new InvalidOperationException("Handler does not have Handle method");
                await (Task)handleMethod.Invoke(handler, [request, cancellationToken])!;
                return Unit.Value;
            };

            // Wrap handler with each behavior - fully compile-time safe
            foreach (ICQRSPipelineBehavior<TRequest, Unit> behavior in behaviors)
            {
                Func<Task<Unit>> currentFunc = handlerFunc;
                handlerFunc = () => behavior.Handle(request, currentFunc, cancellationToken);
            }

            await handlerFunc();
        }
    }

    /// <summary>
    /// Unit type for commands without result
    /// </summary>
    public struct Unit
    {
        public static Unit Value => default;
    }
}
