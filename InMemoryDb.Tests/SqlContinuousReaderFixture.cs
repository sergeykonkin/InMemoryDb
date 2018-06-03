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
    public class SqlContinuousReaderFixture
    {
        [Test]
        public async Task Should_read_all_data()
        {
            // Arrange
            int expected;
            using (var conn = new SqlConnection(Env.ConnectionString))
            {
                expected = (int) conn.ExecuteScalar("SELECT COUNT(*) FROM [User]");
            }

            var reader = new SqlContinuousReader<int, User>(Env.ConnectionString);

            // Act
            int actual = 0;
            reader.NewValue += (key, user) => actual++;
            reader.Start();
            await reader.WhenInitialReadFinished();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public async Task Should_read_new_data()
        {
            // Arrange
            var reader = new SqlContinuousReader<int, User>(Env.ConnectionString);

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
        public async Task Should_respect_attributes()
        {
            // Arrange
            var reader = new SqlContinuousReader<int, User2>(Env.ConnectionString);

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
        public void Should_throw_on_when_no_RowKey_provided()
        {
            // Arrange
            var reader = new SqlContinuousReader<int, User3>(Env.ConnectionString);

            // Act & Assert
            Assert.That(() => reader.Start(),
                Throws.InvalidOperationException.With.Message.StartsWith("Row key column wasn't specified explicitly"));
        }

        [Test]
        public void Should_throw_on_ambiguous_RowKey_attributes()
        {
            // Arrange
            var reader = new SqlContinuousReader<int, User4>(Env.ConnectionString);

            // Act & Assert
            Assert.That(() => reader.Start(),
                Throws.InvalidOperationException.With.Message.EqualTo("Ambiguous multiple [RowKey] attributes."));
        }

        [Test]
        public void Should_throw_on_bad_arguments()
        {
            Assert.That(() => new SqlContinuousReader<int, User>(null), Throws.ArgumentNullException);
            Assert.That(() => new SqlContinuousReader<int, User>(Env.ConnectionString, batchSize: 0),
                Throws.Exception.TypeOf(typeof(ArgumentOutOfRangeException)));
            Assert.That(() => new SqlContinuousReader<int, User>(Env.ConnectionString, delay: 0),
                Throws.Exception.TypeOf(typeof(ArgumentOutOfRangeException)));
            Assert.That(() => new SqlContinuousReader<int, User>(Env.ConnectionString, commandTimeout: -1),
                Throws.Exception.TypeOf(typeof(ArgumentOutOfRangeException)));
        }
    }
}
