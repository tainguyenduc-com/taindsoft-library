namespace TaindSoft.Core.Application.CQRS.Commands
{
    /// <summary>
    /// Handler for commands that produce a result
    /// </summary>
    public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Handler for commands with no result
    /// </summary>
    public interface ICommandHandler<TCommand> where TCommand : ICommand
    {
        Task Handle(TCommand command, CancellationToken cancellationToken = default);
    }
}
