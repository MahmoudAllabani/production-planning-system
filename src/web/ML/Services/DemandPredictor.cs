using Microsoft.ML;
using MLPrediction.MLModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MLPrediction.Services
{
    public class DemandPredictor : IDisposable
    {
        private readonly MLContext _mlContext;
        private readonly ITransformer _model;
        private readonly PredictionEngine<ModelInputProcessed, ModelOutput> _predictionEngine;
        private readonly ITransformer _preprocessingPipeline;
        private bool _disposed = false;

        public DemandPredictor(string modelPath)
        {
            if (!File.Exists(modelPath))
                throw new FileNotFoundException("Eğitilmiş model dosyası bulunamadı", modelPath);

            _mlContext = new MLContext();

            try
            {
                _model = _mlContext.Model.Load(modelPath, out _);
                _preprocessingPipeline = CreatePreprocessingPipeline();
                _predictionEngine = CreatePredictionEngine();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Model yüklenemedi", ex);
            }
        }

        private ITransformer CreatePreprocessingPipeline()
        {
            return _mlContext.Transforms
                .Categorical.OneHotEncoding(inputColumnName: nameof(ModelInputProcessed.UrunId), outputColumnName: "UrunIdEncoded")
                .Append(_mlContext.Transforms.Concatenate("Features",
                    "UrunIdEncoded",
                    nameof(ModelInputProcessed.Year),
                    nameof(ModelInputProcessed.Month),
                    nameof(ModelInputProcessed.Day),
                    nameof(ModelInputProcessed.AvgMiktar),
                    nameof(ModelInputProcessed.AvgMiktar)))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features", "Features"))
                .Fit(_mlContext.Data.LoadFromEnumerable(new[] { new ModelInputProcessed() }));
        }

        public float Predict(int urunId, DateTime siparisTarihi)
        {
            if (_disposed)
                throw new ObjectDisposedException("DemandPredictor");

            var input = new ModelInputProcessed
            {
                UrunId = urunId,
                Year = siparisTarihi.Year,
                Month = siparisTarihi.Month,
                Day = siparisTarihi.Day,
                DayOfWeek = (float)siparisTarihi.DayOfWeek,
                Miktar = 0
            };

            var transformedInput = _preprocessingPipeline.Transform(_mlContext.Data.LoadFromEnumerable(new[] { input }));
            var singleInput = _mlContext.Data.CreateEnumerable<ModelInputProcessed>(transformedInput, reuseRowObject: false).First();
            return PredictInternal(singleInput);
        }

        public List<float> PredictBatch(List<(int UrunId, DateTime Tarih)> requests)
        {
            if (_disposed)
                throw new ObjectDisposedException("DemandPredictor");

            var inputs = requests.Select(req => new ModelInputProcessed
            {
                UrunId = req.UrunId,
                Year = req.Tarih.Year,
                Month = req.Tarih.Month,
                Day = req.Tarih.Day,
                DayOfWeek = (float)req.Tarih.DayOfWeek,
                Miktar = 0
            }).ToList();

            var transformedInputs = _preprocessingPipeline.Transform(_mlContext.Data.LoadFromEnumerable(inputs));
            var predictions = _mlContext.Data.CreateEnumerable<ModelOutput>(_model.Transform(transformedInputs), reuseRowObject: false);
            return predictions.Select(p => p.Score).ToList();
        }

        private float PredictInternal(ModelInputProcessed input)
        {
            try
            {
                var prediction = _predictionEngine.Predict(input);
                return prediction.Score;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Tahmin yapılamadı", ex);
            }
        }

        private PredictionEngine<ModelInputProcessed, ModelOutput> CreatePredictionEngine()
        {
            return _mlContext.Model.CreatePredictionEngine<ModelInputProcessed, ModelOutput>(_model);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _predictionEngine?.Dispose();
                }
                _disposed = true;
            }
        }

        ~DemandPredictor()
        {
            Dispose(false);
        }
    }
} 