using System;
using System.Collections.Generic;

#nullable disable

namespace Kosta.DevOpsChallenge.FileProcessor.WarehouseDb
{
    public partial class Transmission
    {
        public Guid Id { get; set; }
        public DateTime ImportDate { get; set; }
    }
}
