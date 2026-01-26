using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using MLPrediction.MLModel;
using System;
using System.IO;

namespace MLPrediction.Services
{
    public static class ModelTrainer
    {
        public static ModelTrainingResult TrainAndSaveModel(
            string dataPath,
            string modelPath,
            double testFraction = 0.2,
            FastTreeRegressionTrainer.Options trainingOptions = null)
        {
            var mlContext = new MLContext(seed: 42);

            try
            {
                if (!File.Exists(dataPath))
                    throw new FileNotFoundException("Veri dosyası bulunamadı", dataPath);

                var data = DataLoader.LoadAndProcessData(mlContext, dataPath);
                data = mlContext.Data.Cache(data); 

                var split = mlContext.Data.TrainTestSplit(data, testFraction: testFraction);

                var options = trainingOptions ?? new FastTreeRegressionTrainer.Options
                {
                    NumberOfLeaves = 15,
                    NumberOfTrees = 75,
                    MinimumExampleCountPerLeaf = 4,
                    FeatureFraction = 0.8,
                    LearningRate = 0.1,
                    LabelColumnName = "Label",
                    FeatureColumnName = "Features"
           
                };

                var pipeline = BuildTrainingPipeline(mlContext, options);

                Console.WriteLine("Model eğitimi başladı...");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var model = pipeline.Fit(split.TrainSet);

                stopwatch.Stop();
                Console.WriteLine($"Eğitim süresi: {stopwatch.Elapsed.TotalSeconds:F2} Saniye");

                var (metrics, testPredictions) = EvaluateModel(mlContext, model, split.TestSet);
                PrintEvaluationResults(metrics);

                EnsureDirectoryExists(modelPath);
                mlContext.Model.Save(model, split.TrainSet.Schema, modelPath);
                Console.WriteLine($"Model şu konuma kaydedildi: {Path.GetFullPath(modelPath)}");

                return new ModelTrainingResult
                {
                    Model = model,
                    Metrics = metrics,
                    TestDataPredictions = testPredictions
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eğitim sırasında hata oluştu {ex.Message}");
                throw;
            }
        }
        private static IEstimator<ITransformer> BuildTrainingPipeline(
            MLContext mlContext,
            FastTreeRegressionTrainer.Options options)
        {
            return mlContext.Transforms
                .Categorical.OneHotEncoding(inputColumnName: nameof(ModelInputProcessed.UrunId), outputColumnName: "UrunIdEncoded")
                .Append(mlContext.Transforms.Concatenate("Features",
                    "UrunIdEncoded",
                    nameof(ModelInputProcessed.Year),
                    nameof(ModelInputProcessed.Month),
                    nameof(ModelInputProcessed.Day),
                    nameof(ModelInputProcessed.DayOfWeek)))
                .Append(mlContext.Transforms.NormalizeMinMax("Features", "Features"))
                .Append(mlContext.Transforms.CopyColumns("Label", nameof(ModelInputProcessed.Miktar)))
                .Append(mlContext.Regression.Trainers.FastTree(options));
        }

        private static (RegressionMetrics metrics, IDataView predictions) EvaluateModel(
            MLContext mlContext,
            ITransformer model,
            IDataView testData)
        {
            var predictions = model.Transform(testData);
            var metrics = mlContext.Regression.Evaluate(predictions);
            return (metrics, predictions);
        }

        private static void PrintEvaluationResults(RegressionMetrics metrics)
        {
            Console.WriteLine("\nDeğerlendirme Sonuçları:");
            Console.WriteLine($"- R² (Doğruluk): {metrics.RSquared:P2}");
            Console.WriteLine($"- MAE (Ortalama Mutlak Hata): {metrics.MeanAbsoluteError:F2}");
            Console.WriteLine($"- RMSE (Kök Ortalama Kare Hatası): {metrics.RootMeanSquaredError:F2}");
            Console.WriteLine($"- Kayıp (Loss Fonksiyonu): {metrics.LossFunction:F2}\n");
        }

        private static void EnsureDirectoryExists(string modelPath)
        {
            var dir = Path.GetDirectoryName(modelPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
    public class ModelTrainingResult
    {
        public ITransformer? Model { get; set; }
        public RegressionMetrics? Metrics { get; set; }
        public IDataView? TestDataPredictions { get; set; }
    }
}