namespace TaindSoft.Core.Infrastructure.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ConnectionAttribute : Attribute
    {
        public string ConnectionString { get; }
        public string Schema { get; init; }

        public ConnectionAttribute(string connectionString, string schema = "public")
        {
            ConnectionString = connectionString;
            Schema = schema;
        }
    }
}