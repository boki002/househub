namespace househub.Models
{
    // Ez a ViewModel az Error nézethez (Error.cshtml) tartozik.
    // A RequestId a hibához kapcsolódó egyedi azonosítót tárolja.
    public class ErrorViewModel
    {
        // A kérés azonosítója (pl. trace ID)
        public string? RequestId { get; set; }

        // Igaz, ha van érvényes RequestId
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
