namespace Kosta.DevOpsChallenge.FileProcessor.Models
{
    public class ProductTransmission
    {
        public Product[] products { get; set; }
        public TransmissionSummary transmissionsummary { get; set; }

        public bool IsValid()
        {
            return products?.Length > 0 && (transmissionsummary != null);
        }
    }
}
