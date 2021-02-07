using System;

namespace Kosta.DevOpsChallenge.FileProcessor.DtoModel
{
    public class TransmissionSummary
    {
        public Guid id { get; set; }
        public int recordcount { get; set; }
        public int qtysum { get; set; }
    }
}
