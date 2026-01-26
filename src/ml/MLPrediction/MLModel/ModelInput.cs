using Microsoft.ML.Data;

namespace MLPrediction.MLModel
{
    public class ModelInput
    {
        [LoadColumn(0)]
        public float UrunId { get; set; }

        [LoadColumn(1)]
        public string SiparisTarihi { get; set; }

        [LoadColumn(2)]
        public float Miktar { get; set; }
    }

    public class ModelInputProcessed
    {
        public float UrunId { get; set; }
        public float Year { get; set; }
        public float Month { get; set; }
        public float Day { get; set; }
        public float DayOfWeek { get; set; }
        public float Miktar { get; set; }
        public float AvgMiktar { get; set; }
    }
}


