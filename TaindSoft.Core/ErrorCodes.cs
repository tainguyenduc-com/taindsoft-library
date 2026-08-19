namespace TaindSoft.Core
{
    /// <summary>
    /// TODO: Document class ErrorCodes
    /// </summary>
    public static class ErrorCodes
    {
        // Core-level (uncategorized) codes use COM prefix
        public const string Unknown = "COM9999"; // newly defined unknown/uncontrolled error

        public const string InvalidArgument = "COM0001"; // previously "InvalidArgument"
        public const string InvalidOperation = "COM0002"; // previously "InvalidOperation"
        public const string Unauthorized = "COM0003"; // previously "Unauthorized"
        public const string NotFound = "COM0004"; // previously "NotFound"
        public const string InternalServerError = "COM0005"; // previously "InternalServerError"
        public const string ValidationFailed = "COM0006"; // previously "ValidationFailed"
        public const string BusinessRuleViolation = "COM0007"; // newly defined business rule violation
        public const string ConcurrencyConflict = "COM0010"; // concurrency conflict
    }
}
