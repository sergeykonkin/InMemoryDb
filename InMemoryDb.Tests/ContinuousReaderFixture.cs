using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;

namespace InMemoryDb.Tests
{
    [TestFixture]
    public class ContinuousReaderFixture
    {
        [Test]
        public async Task Should_read_all_data()
        {
            // Arrange
            var batchReader = new SqlBatchReader<User>(Env.ConnectionString);
            var reader = new ContinuousReader<User>(batchReader);

            // Act
            int actual = 0;
            reader.NewValue += (key, user) => actual++;
            reader.Start();
            await reader.WhenInitialReadFinished();

            // Assert
            var expected = new SqlConnection(Env.ConnectionString).ExecuteScalar<int>("SELECT COUNT(*) FROM [User]");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public async Task Should_read_new_data()
        {
            // Arrange
            var batchReader = new SqlBatchReader<User>(Env.ConnectionString);
            var reader = new ContinuousReader<User>(batchReader);

            // Act
            bool read = false;
            reader.Start();
            await reader.WhenInitialReadFinished();

            reader.NewValue += (key, user) => read = true;

            using (var conn = new SqlConnection(Env.ConnectionString))
            {
                conn.Execute(
                    @"INSERT INTO [User] (FirstName, LastName, Age, Gender)
                          VALUES (@firstName, @lastName, @age, @gender);",
                    Env.UserFaker.Generate());
            }

            await Task.Delay(400); // doubled default delay (200ms)

            // Assert
            Assert.IsTrue(read);
        }

        [Test]
        public async Task Should_read_updated_data()
        {
            // Arrange
            var batchReader = new SqlTimestampBatchReader<User>(Env.ConnectionString, "_ts");
            var reader = new ContinuousReader<User>(batchReader);

            // Act
            bool read = false;
            int id = 0;
            string newName = null;
            reader.Start();
            await reader.WhenInitialReadFinished();

            reader.NewValue += (key, user) =>
            {
                read = true;
                id = user.Id;
                newName = user.FirstName;
            };

            new SqlConnection(Env.ConnectionString).Execute(
                @"UPDATE [User]
                  SET FirstName = 'Sereja'
                  WHERE Id = 1488");

            await Task.Delay(400); // doubled default delay (200ms)

            // Assert
            Assert.IsTrue(read);
            Assert.AreEqual(1488, id);
            Assert.AreEqual("Sereja", newName);
        }

        [Test]
        public async Task Should_respect_attributes()
        {
            // Arrange
            var batchReader = new SqlBatchReader<User2>(Env.ConnectionString);
            var reader = new ContinuousReader<User2>(batchReader);

            // Act
            var actual = new ConcurrentBag<User2>();
            reader.NewValue += (key, user) => actual.Add(user);
            reader.Start();
            await reader.WhenInitialReadFinished();

            // Assert
            Assert.IsTrue(actual.All(u => u.Id2 != 0));
            Assert.IsTrue(actual.All(u => u.FirstName2 != null));
            Assert.IsTrue(actual.All(u => u.LastName2 != null));
            Assert.IsFalse(actual.All(u => u.Gender2 == false));
            Assert.IsFalse(actual.All(u => u.Gender2 == true));
            Assert.IsTrue(actual.All(u => u.Age2 != 0));
        }

        [Test]
        public void Should_throw_on_bad_arguments()
        {
            Assert.That(() => new ContinuousReader<User>(null), Throws.ArgumentNullException);
            Assert.That(() => new ContinuousReader<User>(new SqlBatchReader<User>(Env.ConnectionString), delay: 0),
                Throws.Exception.TypeOf(typeof(ArgumentOutOfRangeException)));
        }
    }
}
