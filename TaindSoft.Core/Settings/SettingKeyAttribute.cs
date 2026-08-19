namespace TaindSoft.Core.Settings
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    /// <summary>
    /// TODO: Document class SettingKeyAttribute
    /// </summary>
    public sealed class SettingKeyAttribute(string key) : Attribute
    {
        public string Key { get; } = key;
    }
}
