using System;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using NUnit.Framework;

namespace InMemoryDb.Tests
{
    public class SqlTimestampReaderFixture
    {
        [Test]
        public void Should_read_all_data()
        {
            // Arrange
            var reader = new SqlTimestampReader<User>(Env.ConnectionString);

            // Act
            var actual = reader.Read(0ul).ToList().Count;

            // Assert
            var expected = new SqlConnection(Env.ConnectionString).ExecuteScalar<int>("SELECT COUNT(*) FROM [User]");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Should_respect_Table_and_Column_attributes()
        {
            // Arrange
            var reader = new SqlTimestampReader<User2>(Env.ConnectionString);

            // Act
            var actual = reader.Read(0ul).Select(t => t.Item2).ToList();

            // Assert
            Assert.IsTrue(actual.All(u => u.Id2 != 0));
            Assert.IsTrue(actual.All(u => u.FirstName2 != null));
            Assert.IsTrue(actual.All(u => u.LastName2 != null));
            Assert.IsFalse(actual.All(u => u.Gender2 == false));
            Assert.IsFalse(actual.All(u => u.Gender2 == true));
            Assert.IsTrue(actual.All(u => u.Age2 != 0));
        }

        [Test]
        public void Should_respect_NotMapped_attribute()
        {
            // without [Ignore]
            Assert.That(() => new SqlTimestampReader<User3>(Env.ConnectionString).Read(0ul).ToList(),
                Throws.Exception.TypeOf<IndexOutOfRangeException>().With.Message.EqualTo(nameof(User3.FullName)));

            // with[Ignore]
            Assert.DoesNotThrow(() => new SqlTimestampReader<User4>(Env.ConnectionString).Read(0ul).ToList());
        }

        [Test]
        public void Should_throw_on_bad_arguments()
        {
            Assert.That(() => new SqlTimestampReader<User>(null), Throws.ArgumentNullException);
            Assert.That(() => new SqlTimestampReader<User>(Env.ConnectionString, null), Throws.ArgumentNullException);
            Assert.That(() => new SqlTimestampReader<User>(Env.ConnectionString, commandTimeout: -1),
                Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SqlTimestampReader<User>(Env.ConnectionString, batchSize: 0),
                Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
