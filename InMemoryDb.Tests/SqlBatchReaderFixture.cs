using System;
using System.Linq;
using NUnit.Framework;

namespace InMemoryDb.Tests
{
    [TestFixture]
    public class SqlBatchReaderFixture
    {
        [Test]
        public void Should_read_by_batches()
        {
            // Arrange
            var reader = new SqlBatchReader<User>(Env.ConnectionString);

            // Act
            var batch1 = reader.ReadNextBatch(0).ToList();
            var batch2 = reader.ReadNextBatch(1000).ToList();

            // Assert
            Assert.AreEqual(1000, batch1.Count);
            Assert.AreEqual(1, batch1.Min(t => t.Item1));
            Assert.AreEqual(1000, batch1.Max(t => t.Item1));

            Assert.AreEqual(1000, batch2.Count);
            Assert.AreEqual(1001, batch2.Min(t => t.Item1));
            Assert.AreEqual(2000, batch2.Max(t => t.Item1));
        }

        [Test]
        public void Should_throw_on_when_no_RowKey_provided()
        {
            Assert.That(() => new SqlBatchReader<User3>(Env.ConnectionString),
                Throws.InvalidOperationException.With.Message.StartsWith("Row key column wasn't specified explicitly"));
        }

        [Test]
        public void Should_throw_on_ambiguous_RowKey_attributes()
        {
            Assert.That(() => new SqlBatchReader<User4>(Env.ConnectionString),
                Throws.InvalidOperationException.With.Message.EqualTo("Ambiguous multiple [RowKey] attributes."));
        }

        [Test]
        public void Should_throw_on_bad_arguments()
        {
            Assert.That(() => new SqlBatchReader<User>(null), Throws.ArgumentNullException);
            Assert.That(() => new SqlBatchReader<User>(Env.ConnectionString, commandTimeout: -1),
                Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SqlBatchReader<User>(Env.ConnectionString, batchSize: 0),
                Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
