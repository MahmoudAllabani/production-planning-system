using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLPrediction.MLModel;
using System.IO;
using System.Globalization;

namespace MLPrediction.Services
{
    public static class DataLoader
    {
        public static IDataView LoadAndProcessData(MLContext mlContext, string dataPath)
        {
            if (!File.Exists(dataPath))
                throw new FileNotFoundException($"Dosya Bulunamadı: {dataPath}");

            var originalData = mlContext.Data.LoadFromTextFile<ModelInput>(
                path: dataPath,
                hasHeader: true,
                separatorChar: ';');

            var processed = mlContext.Data.CreateEnumerable<ModelInput>(originalData, reuseRowObject: false)
                .GroupBy(row => row.UrunId)
                .SelectMany(group => group.Select((row, i) => new ModelInputProcessed
                {
                    UrunId = row.UrunId,
                    Year = DateTime.ParseExact(row.SiparisTarihi, "dd/MM/yyyy", CultureInfo.InvariantCulture).Year,
                    Month = DateTime.ParseExact(row.SiparisTarihi, "dd/MM/yyyy", CultureInfo.InvariantCulture).Month,
                    Day = DateTime.ParseExact(row.SiparisTarihi, "dd/MM/yyyy", CultureInfo.InvariantCulture).Day,
                    DayOfWeek = (float)DateTime.ParseExact(row.SiparisTarihi, "dd/MM/yyyy", CultureInfo.InvariantCulture).DayOfWeek,
                    Miktar = row.Miktar,
                    AvgMiktar = group.Take(i + 1).Average(r => r.Miktar)
                }))
                .Where(row => row != null);

            return mlContext.Data.LoadFromEnumerable<ModelInputProcessed>(processed);

        }
    }
}



