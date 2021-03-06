﻿//using System.Data.SqlClient;
//using System.Threading.Tasks;
//using Dapper;
//using NUnit.Framework;

//namespace InMemoryDb.Tests
//{
//    [TestFixture]
//    public class InMemoryReplicaFixture
//    {
//        [Test]
//        public async Task Should_read_all_data_to_dictionary()
//        {
//            // Arrange
//            var conn = new SqlConnection(Env.ConnectionString);
//            var reader = new ContinuousReader<User>(new SqlTimestampReader<User>(Env.ConnectionString));
//            var replica = new InMemoryTable<int, User>(reader, user => user.Id);

//            // Act
//            reader.Start();
//            await replica.WhenInitialReadFinished();
//            var actual = replica.Count;

//            // Assert
//            var expected = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM [User] WHERE Deleted = 0");
//            Assert.AreEqual(expected, actual);
//        }

//        [Test]
//        public async Task Should_read_all_data_to_bag()
//        {
//            // Arrange
//            var conn = new SqlConnection(Env.ConnectionString);
//            var reader = new ContinuousReader<User>(new SqlTimestampReader<User>(Env.ConnectionString));
//            var replica = new InMemoryTable<User>(reader);

//            // Act
//            reader.Start();
//            await replica.WhenInitialReadFinished();
//            var actual = replica.Count;

//            // Assert
//            var expected = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM [User] WHERE Deleted = 0");
//            Assert.AreEqual(expected, actual);
//        }

//        [Test]
//        public async Task Should_build_custom_index_when_reading_by_timestamps()
//        {
//            // Arrange
//            var conn = new SqlConnection(Env.ConnectionString);
//            var expected = conn.QuerySingle<User>("SELECT * FROM [User] WHERE [Id] = 1337");
//            var reader = new ContinuousReader<User>(new SqlTimestampReader<User>(Env.ConnectionString));
//            var replica = new InMemoryTable<int, User>(reader, user => user.Id);

//            // Act
//            reader.Start();
//            await replica.WhenInitialReadFinished();

//            // Assert
//            Assert.AreEqual(expected, replica[1337]);
//        }
//    }
//}
