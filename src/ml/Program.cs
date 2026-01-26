using System;
using System.Globalization;
using System.IO;
using MLPrediction.MLModel;
using MLPrediction.Services;

namespace MLPrediction
{
    class Program
    {
        static void Main(string[] args)
        {
            var dataDir = Path.Combine(Environment.CurrentDirectory, "Data");
            var dataPath = Path.Combine(dataDir, "orders_data.csv");
            var modelPath = Path.Combine(dataDir, "DemandPredictionModel.zip");

            EnsureDirectoryExists(dataDir);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("          TALEP TAHMİN UYGULAMASINA HOŞ GELDİNİZ  ");
            Console.WriteLine("--------------------------------------------------");
            Console.ResetColor();

            TrainModel(dataPath, modelPath);

            Console.WriteLine("\n"); // Add a new line for better spacing

            RunPredictions(modelPath);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n--------------------------------------------------");
            Console.WriteLine("          UYGULAMADAN ÇIKILIYOR                   ");
            Console.WriteLine("--------------------------------------------------");
            Console.ResetColor();
        }

        private static void TrainModel(string dataPath, string modelPath)
        {
            try
            {
                if (!File.Exists(dataPath))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Uyarı: Veri dosyası mevcut değil. Eğitim mümkün değil. Lütfen 'orders_data.csv' dosyasını 'Data' klasörüne yerleştirin.");
                    Console.ResetColor();
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Model eğitimi başlatılıyor...");
                Console.ResetColor();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                ModelTrainer.TrainAndSaveModel(dataPath, modelPath);

                stopwatch.Stop();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Model {stopwatch.Elapsed.TotalSeconds:F2} saniyede başarıyla eğitildi!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Hata: Eğitim başarısız oldu: {ex.Message}");
                Console.WriteLine("Teknik detaylar için aşağıdaki hata izini kontrol edin:");
                Console.ResetColor();
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void RunPredictions(string modelPath)
        {
            try
            {
                if (!File.Exists(modelPath))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Uyarı: Model eğitilmemiş. Lütfen önce modeli eğitin.");
                    Console.ResetColor();
                    return;
                }

                using var predictor = new DemandPredictor(modelPath);
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine("          TALEP TAHMİN SİSTEMİ                    ");
                Console.WriteLine("          Çıkmak için 'exit' yazın                ");
                Console.WriteLine("--------------------------------------------------");
                Console.ResetColor();

                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("\nTahmin verilerini girin:");
                    Console.WriteLine("Örnek: Ürün ID ve Tarih (GG/AA/YYYY) formatında");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Örnek Giriş: 3 10/05/2024");
                    Console.ResetColor();

                    Console.Write("> ");
                    var input = Console.ReadLine();
                    if (input?.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) ?? false) break;

                    if (TryParseInput(input, out int productId, out DateTime date))
                    {
                        try
                        {
                            var prediction = predictor.Predict(productId, date);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\nTahmin Sonucu:");
                            Console.WriteLine($"   Tarih: {date:dd/MM/yyyy}");
                            Console.WriteLine($"   Ürün ID: {productId}");
                            Console.WriteLine($"   Beklenen Talep: {prediction:F2} birim");
                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Hata: Tahmin sırasında bir sorun oluştu: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Hata: Yanlış format! Lütfen şu formatı kullanın: [Ürün Numarası] [Gün/Ay/Yıl]");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Hata: Tahmin sistemi başlatılamadı: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static bool TryParseInput(string input, out int productId, out DateTime date)
        {
            productId = 0;
            date = DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(input)) return false;

            var parts = input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;

            return int.TryParse(parts[0], out productId) &&
                   DateTime.TryParseExact(parts[1], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"'Data' klasörü oluşturuldu: {path}");
                Console.ResetColor();
            }
        }
    }
}