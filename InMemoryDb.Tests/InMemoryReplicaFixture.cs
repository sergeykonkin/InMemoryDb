using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;

namespace InMemoryDb.Tests
{
    [TestFixture]
    public class InMemoryReplicaFixture
    {
        [Test]
        public async Task Should_read_all_data_to_dictionary()
        {
            // Arrange
            var conn = new SqlConnection(Env.ConnectionString);
            var expected = (int)conn.ExecuteScalar("SELECT COUNT(*) FROM [User]");
            var reader = new ContinuousReader<User>(new SqlBatchReader<User>(Env.ConnectionString));
            var table = new InMemoryReplica<int, User>(reader);

            // Act
            reader.Start();
            await table.WhenInitialReadFinished();

            // Assert
            Assert.AreEqual(expected, table.Count);
        }

        [Test]
        public async Task Should_read_all_data_to_bag()
        {
            // Arrange
            var conn = new SqlConnection(Env.ConnectionString);
            var expected = (int)conn.ExecuteScalar("SELECT COUNT(*) FROM [User]");
            var reader = new ContinuousReader<User>(new SqlBatchReader<User>(Env.ConnectionString));
            var table = new InMemoryReplica<User>(reader);

            // Act
            reader.Start();
            await table.WhenInitialReadFinished();

            // Assert
            Assert.AreEqual(expected, table.Count);
        }

        [Test]
        public async Task Should_build_custom_index_when_reading_by_timestamps()
        {
            // Arrange
            var conn = new SqlConnection(Env.ConnectionString);
            var expected = conn.QuerySingle<User>("SELECT * FROM [User] WHERE [Id] = 1337");
            var reader = new ContinuousReader<User>(new SqlTimestampBatchReader<User>(Env.ConnectionString, "_ts"));
            var table = new InMemoryReplica<int, User>(reader, user => user.Id);

            // Act
            reader.Start();
            await table.WhenInitialReadFinished();

            // Assert
            Assert.AreEqual(expected, table[1337]);
        }
    }
}
