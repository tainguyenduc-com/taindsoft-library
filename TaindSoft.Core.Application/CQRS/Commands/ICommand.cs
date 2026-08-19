namespace TaindSoft.Core.Application.CQRS.Commands
{
    /// <summary>
    /// Represents a command that produces a result
    /// </summary>
    public interface ICommand<TResult> : ICommand
    {
    }

    /// <summary>
    /// Represents a command with no result
    /// </summary>
    public interface ICommand
    {
    }
}
