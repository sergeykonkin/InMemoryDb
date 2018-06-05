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
        public void Should_respect_Table_and_Column_attributes()
        {
            // Arrange
            var reader = new SqlBatchReader<User2>(Env.ConnectionString);

            // Act
            var actual = reader.ReadNextBatch(0).Select(t => t.Item2).ToList();

            // Assert
            Assert.IsTrue(actual.All(u => u.Id2 != 0));
            Assert.IsTrue(actual.All(u => u.FirstName2 != null));
            Assert.IsTrue(actual.All(u => u.LastName2 != null));
            Assert.IsFalse(actual.All(u => u.Gender2 == false));
            Assert.IsFalse(actual.All(u => u.Gender2 == true));
            Assert.IsTrue(actual.All(u => u.Age2 != 0));
        }

        [Test]
        public void Should_respect_Ignore_attribute()
        {
            // without [Ignore]
            Assert.That(() => new SqlBatchReader<User5>(Env.ConnectionString).ReadNextBatch(0).ToList(),
                Throws.Exception.TypeOf<IndexOutOfRangeException>().With.Message.EqualTo(nameof(User5.FullName)));

            // with[Ignore]
            Assert.DoesNotThrow(() => new SqlBatchReader<User6>(Env.ConnectionString).ReadNextBatch(0).ToList());
        }

        [Test]
        public void Should_throw_on_when_no_RowKey_provided()
        {
            Assert.That(() => new SqlBatchReader<User3>(Env.ConnectionString).ReadNextBatch(0).ToList(),
                Throws.InvalidOperationException.With.Message.StartsWith("Row key column wasn't specified explicitly"));
        }

        [Test]
        public void Should_throw_on_ambiguous_RowKey_attributes()
        {
            Assert.That(() => new SqlBatchReader<User4>(Env.ConnectionString).ReadNextBatch(0).ToList(),
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
