// DbSeeder.cs
// Generates and inserts 2,400+ realistic mock agricultural records into SQL Server.
// This simulates a "Big Data" historical dataset spanning 24 months.
// Records are inserted in batches for performance — avoids memory spikes on large inserts.

using AgriAnalytics.API.Domain.Entities;

namespace AgriAnalytics.API.Infrastructure.Persistence
{
    public static class DbSeeder
    {
        // Crop profiles: each entry defines the realistic environmental ranges
        // [temp_mean, temp_std, humidity_mean, humidity_std,
        //  ph_mean, ph_std, rainfall_mean, rainfall_std]
        private static readonly Dictionary<string, float[]> CropProfiles = new()
        {
            { "Wheat",     new[] { 22f, 4f,  45f, 10f, 6.5f, 0.5f, 300f,  60f } },
            { "Rice",      new[] { 28f, 3f,  82f,  8f, 6.0f, 0.4f, 950f, 100f } },
            { "Maize",     new[] { 26f, 4f,  65f, 10f, 6.2f, 0.5f, 550f,  80f } },
            { "Soybean",   new[] { 24f, 3f,  68f,  9f, 6.8f, 0.4f, 500f,  70f } },
            { "Cotton",    new[] { 30f, 4f,  55f, 10f, 7.0f, 0.5f, 400f,  80f } },
            { "Sugarcane", new[] { 32f, 3f,  75f,  8f, 6.5f, 0.4f, 850f,  90f } },
            { "Coffee",    new[] { 25f, 2f,  78f,  7f, 6.3f, 0.3f, 700f,  80f } },
            { "Potato",    new[] { 18f, 3f,  70f,  8f, 5.8f, 0.4f, 450f,  70f } },
            { "Tomato",    new[] { 27f, 3f,  72f,  8f, 6.4f, 0.4f, 400f,  60f } },
            { "Barley",    new[] { 20f, 4f,  50f, 10f, 7.2f, 0.5f, 280f,  55f } },
        };

        private const int BATCH_SIZE = 500;
        private const int RECORDS_PER_CROP_PER_MONTH = 10; // 10 crops × 24 months × 10 = 2,400 records

        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            // Skip seeding if data already exists — prevents duplicate inserts on restart
            if (context.AgriDataRecords.Any())
            {
                logger.LogInformation("Database already contains {Count} records. Skipping seed.",
                    context.AgriDataRecords.Count());
                return;
            }

            logger.LogInformation("Starting database seeding...");

            var random = new Random(42); // Fixed seed for reproducibility
            var records = new List<AgriDataRecord>();

            // Generate records spanning 24 months back from today
            var startDate = DateTime.UtcNow.AddMonths(-24);

            foreach (var (cropName, profile) in CropProfiles)
            {
                float tempMean = profile[0], tempStd = profile[1];
                float humMean = profile[2], humStd = profile[3];
                float phMean = profile[4], phStd = profile[5];
                float rainMean = profile[6], rainStd = profile[7];

                // Generate records for each month in the 24-month window
                for (int monthOffset = 0; monthOffset < 24; monthOffset++)
                {
                    var monthStart = startDate.AddMonths(monthOffset);

                    for (int i = 0; i < RECORDS_PER_CROP_PER_MONTH; i++)
                    {
                        // Apply seasonal temperature variation (sine wave over 12 months)
                        float seasonalOffset = (float)(2.5 * Math.Sin(2 * Math.PI * monthOffset / 12.0));

                        var record = new AgriDataRecord
                        {
                            Temperature = Clamp(SampleGaussian(random, tempMean + seasonalOffset, tempStd), 5f, 45f),
                            Humidity = Clamp(SampleGaussian(random, humMean, humStd), 20f, 100f),
                            Soil_pH = Clamp(SampleGaussian(random, phMean, phStd), 4.0f, 9.0f),
                            Rainfall = Clamp(SampleGaussian(random, rainMean, rainStd), 50f, 1200f),
                            CropLabel = cropName,

                            // Spread records randomly within the month
                            DateRecorded = monthStart.AddDays(random.Next(0, 28))
                                                     .AddHours(random.Next(0, 24))
                                                     .AddMinutes(random.Next(0, 60))
                        };

                        records.Add(record);
                    }
                }
            }

            // Shuffle records to mix crops and dates — more realistic dataset
            records = records.OrderBy(_ => random.Next()).ToList();

            // Insert in batches to avoid memory pressure on large inserts
            int totalInserted = 0;
            for (int i = 0; i < records.Count; i += BATCH_SIZE)
            {
                var batch = records.Skip(i).Take(BATCH_SIZE).ToList();
                await context.AgriDataRecords.AddRangeAsync(batch);
                await context.SaveChangesAsync();
                totalInserted += batch.Count;
                logger.LogInformation("Inserted batch: {Inserted}/{Total} records", totalInserted, records.Count);
            }

            logger.LogInformation("Seeding complete. Total records inserted: {Total}", totalInserted);
        }

        // Box-Muller transform — generates normally distributed random values
        private static float SampleGaussian(Random rng, float mean, float stdDev)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return (float)(mean + stdDev * randStdNormal);
        }

        // Keeps a value within [min, max] bounds
        private static float Clamp(float value, float min, float max)
            => Math.Max(min, Math.Min(max, value));
    }
}