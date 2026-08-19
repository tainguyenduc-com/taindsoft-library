namespace TaindSoft.AdminUI.Models
{
    /// <summary>
    /// TODO: Document class ValidationError
    /// </summary>
    public class ValidationError
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
    }
}
