using System;
using System.Linq;
using NUnit.Framework;

namespace InMemoryDb.Tests
{
    public class SqlBatchTimestampReaderFixture
    {
        [Test]
        public void Should_read_by_batches()
        {
            // Arrange
            var reader = new SqlTimestampBatchReader<User>(Env.ConnectionString, "_ts");

            // Act
            var batch1 = reader.ReadNextBatch(0).ToList();
            var batch2 = reader.ReadNextBatch(batch1.Max(t => t.Item1)).ToList();

            // Assert
            Assert.AreEqual(1000, batch1.Count);
            Assert.AreEqual(1000, batch2.Count);
            Assert.Greater(batch2.Min(t => t.Item1), batch1.Min(t => t.Item1));
        }

        [Test]
        public void Should_throw_on_bad_arguments()
        {
            Assert.That(() => new SqlTimestampBatchReader<User>(null, "_ts"), Throws.ArgumentNullException);
            Assert.That(() => new SqlTimestampBatchReader<User>(Env.ConnectionString, null), Throws.ArgumentNullException);
            Assert.That(() => new SqlTimestampBatchReader<User>(Env.ConnectionString, "_ts", commandTimeout: -1),
                Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SqlTimestampBatchReader<User>(Env.ConnectionString,"_ts", batchSize: 0),
                Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
