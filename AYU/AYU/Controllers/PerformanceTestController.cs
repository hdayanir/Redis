using AYU.Data;
using AYU.Models;
using MessagePack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProtoBuf;
using SolTechnology.Avro;
using StackExchange.Redis;
using System.Text.Json;

namespace AYU.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PerformanceTestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDatabase _redisDb;

        public PerformanceTestController(AppDbContext context, IConnectionMultiplexer redis)
        {
            _context = context;
            _redisDb = redis.GetDatabase();
        }

        /// <summary>
        /// Senaryo 1: Veritabanından (SQL Server) Doğrudan Okuma (Referans Noktası)
        /// </summary>
        /// <remarks>
        /// Bu servis, herhangi bir önbellekleme mekanizması kullanmadan veriyi doğrudan ilişkisel veritabanından çeker. 
        /// Performans testlerinde "en düşük" hız referansı (baseline) olarak kabul edilir.
        /// </remarks>
        [HttpGet("scenario1-sql")]
        public async Task<IActionResult> GetFromSql()
        {
            var data = await _context.Transactions.AsNoTracking().ToListAsync();
            return Ok($"SQL'den okunan kayıt sayısı: {data.Count}");
        }

        /// <summary>
        /// Senaryo 2: Redis Üzerinden JSON Formatında Okuma
        /// </summary>
        /// <remarks>
        /// Veriyi Redis önbelleğinden metin tabanlı JSON formatında getirir. 
        /// JSON, okunabilirlik avantajı sunsa da yüksek trafikli sistemlerde büyük veri setleri için RAM ve CPU maliyeti oluşturmaktadır.
        /// </remarks>
        [HttpGet("scenario2-redis-json")]
        public async Task<IActionResult> GetFromRedisJson()
        {
            string cacheKey = "transactions_json";
            var cachedData = await _redisDb.StringGetAsync(cacheKey);

            if (cachedData.HasValue)
            {
                var data = JsonSerializer.Deserialize<List<BankTransaction>>(cachedData.ToString()!);
                return Ok($"Redis (JSON) üzerinden okunan kayıt sayısı: {data?.Count ?? 0}");
            }

            var dbData = await _context.Transactions.AsNoTracking().ToListAsync();
            var serializedData = JsonSerializer.Serialize(dbData);
            await _redisDb.StringSetAsync(cacheKey, serializedData);

            return Ok($"Veri SQL'den çekildi ve Redis'e JSON olarak yazıldı. Kayıt sayısı: {dbData.Count}");
        }

        /// <summary>
        /// Senaryo 3: Redis Üzerinden MessagePack Formatında Okuma
        /// </summary>
        /// <remarks>
        /// Veriyi Redis'ten MessagePack formatında getirir. 
        /// İkili sıkıştırma sayesinde JSON'a kıyasla daha düşük ağ gecikmesi ve daha az RAM tüketimi hedeflenmektedir.
        /// </remarks>
        [HttpGet("scenario3-redis-msgpack")]
        public async Task<IActionResult> GetFromRedisMsgPack()
        {
            string cacheKey = "transactions_msgpack";
            var cachedData = await _redisDb.StringGetAsync(cacheKey);
            var options = MessagePackSerializerOptions.Standard.WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            if (cachedData.HasValue)
            {
                byte[] bytes = (byte[])cachedData!;
                var data = MessagePackSerializer.Deserialize<List<BankTransaction>>(bytes, options);
                return Ok($"Redis (MessagePack) üzerinden okunan kayıt sayısı: {data?.Count ?? 0}");
            }

            var dbData = await _context.Transactions.AsNoTracking().ToListAsync();
            var serializedData = MessagePackSerializer.Serialize(dbData, options);
            await _redisDb.StringSetAsync(cacheKey, serializedData);

            return Ok($"Veri SQL'den çekildi ve Redis'e MessagePack olarak yazıldı. Kayıt sayısı: {dbData.Count}");
        }

        /// <summary>
        /// Senaryo 4: Redis Üzerinden gRPC (Protocol Buffers) Formatında Okuma
        /// </summary>
        /// <remarks>
        /// Google tarafından geliştirilen Protobuf protokolünü kullanarak veriyi ikili formda okur. 
        /// gRPC mimarisinin temelini oluşturan bu yöntem, şemaya dayalı yapısı ile en yüksek hızlardan birini vaat eder.
        /// </remarks>
        [HttpGet("scenario4-redis-protobuf")]
        public async Task<IActionResult> GetFromRedisProtobuf()
        {
            string cacheKey = "transactions_protobuf";
            var cachedData = await _redisDb.StringGetAsync(cacheKey);

            if (cachedData.HasValue)
            {
                byte[] bytes = (byte[])cachedData!;
                using var stream = new MemoryStream(bytes);
                var data = Serializer.Deserialize<List<BankTransaction>>(stream);
                return Ok($"Redis (Protobuf) üzerinden okunan kayıt sayısı: {data?.Count ?? 0}");
            }

            var dbData = await _context.Transactions.AsNoTracking().ToListAsync();
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, dbData);
                await _redisDb.StringSetAsync(cacheKey, stream.ToArray());
            }

            return Ok($"Veri SQL'den çekildi ve Redis'e Protobuf olarak yazıldı. Kayıt sayısı: {dbData.Count}");
        }

        /// <summary>
        /// Senaryo 5: Redis Üzerinden Apache Avro Formatında Okuma
        /// </summary>
        /// <remarks>
        /// Büyük veri sistemlerinde standart olan Apache Avro formatını kullanır. 
        /// Veriyi şemasıyla birlikte kompakt bir ikili yapıda saklayarak yüksek performanslı serileştirme sağlar.
        /// </remarks>
        [HttpGet("scenario5-redis-avro")]
        public async Task<IActionResult> GetFromRedisAvro()
        {
            string cacheKey = "transactions_avro";
            var cachedData = await _redisDb.StringGetAsync(cacheKey);

            if (cachedData.HasValue)
            {
                byte[] bytes = (byte[])cachedData!;
                var data = AvroConvert.Deserialize<List<BankTransaction>>(bytes);
                return Ok($"Redis (Avro) üzerinden okunan kayıt sayısı: {data?.Count ?? 0}");
            }

            var dbData = await _context.Transactions.AsNoTracking().ToListAsync();
            var avroBytes = AvroConvert.Serialize(dbData);
            await _redisDb.StringSetAsync(cacheKey, avroBytes);

            return Ok($"Veri SQL'den çekildi ve Redis'e Apache Avro olarak yazıldı. Kayıt sayısı: {dbData.Count}");
        }

        /// <summary>
        /// Veritabanına 100.000 satırlık test verisi ekler.
        /// </summary>
        /// <remarks>
        /// Bellek taşmalarını önlemek için veriler 10.000'lik paketler halinde SQL'e yazılır.
        /// </remarks>
        [HttpPost("seed-100k-data")]
        public async Task<IActionResult> Seed100kData()
        {
            int currentCount = await _context.Transactions.CountAsync();
            if (currentCount >= 100000)
            {
                return Ok($"Veritabanında zaten {currentCount} kayıt var. Yeni veri üretilmesine gerek yok.");
            }

            int recordsToGenerate = 100000 - currentCount;
            int batchSize = 10000;
            var random = new Random();

            for (int i = 0; i < recordsToGenerate; i += batchSize)
            {
                var transactions = new List<BankTransaction>();
                int currentBatchSize = Math.Min(batchSize, recordsToGenerate - i);

                for (int j = 0; j < currentBatchSize; j++)
                {
                    transactions.Add(new BankTransaction
                    {
                        TransactionReference = Guid.NewGuid(),
                        SenderAccount = $"TR{random.Next(10, 99)}000{random.Next(1000000, 9999999)}",
                        ReceiverAccount = $"TR{random.Next(10, 99)}000{random.Next(1000000, 9999999)}",
                        Amount = (decimal)(random.NextDouble() * 10000),
                        Currency = "TRY",
                        TransactionDate = DateTime.Now.AddMinutes(-random.Next(1, 500000)),
                        Description = "Test Transfer",
                        IsSuccessful = random.Next(0, 2) == 1
                    });
                }

                await _context.Transactions.AddRangeAsync(transactions);
                await _context.SaveChangesAsync();

                _context.ChangeTracker.Clear();
            }

            int finalCount = await _context.Transactions.CountAsync();
            return Ok($"Başarılı! Veritabanına toplam {recordsToGenerate} yeni kayıt eklendi. Toplam kayıt sayısı: {finalCount}");
        }
    }
}