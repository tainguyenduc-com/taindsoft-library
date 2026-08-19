namespace TaindSoft.Core.Infrastructure.Seeding
{
    /// <summary>
    /// TODO: Document interface ISeeder
    /// </summary>
    public interface ISeeder
    {
        /// <summary>
        /// Seed data using the provided service provider.
        /// </summary>
        Task SeedAsync(IServiceProvider provider, CancellationToken cancellationToken = default);
    }
}
