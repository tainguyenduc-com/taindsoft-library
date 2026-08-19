namespace TaindSoft.Core.Application.CQRS.Commands
{
    /// <summary>
    /// Base handler for commands that produce a result
    /// </summary>
    /// <typeparam name="TCommand">Command type</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    public abstract class CommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        /// <summary>
        /// Handle the command
        /// </summary>
        public abstract Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Base handler for commands without result
    /// </summary>
    /// <typeparam name="TCommand">Command type</typeparam>
    public abstract class CommandHandler<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Handle the command
        /// </summary>
        public abstract Task Handle(TCommand command, CancellationToken cancellationToken = default);
    }
}
