using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;

namespace InMemoryDb.Tests
{
    [TestFixture]
    public class SqlContinuousTimestampReaderFixture
    {
        [Test]
        public async Task Should_read_all_data()
        {
            // Arrange
            var conn = new SqlConnection(Env.ConnectionString);
            var expected = (int) conn.ExecuteScalar("SELECT COUNT(*) FROM [User]");
            var reader = new SqlContinuousTimestampReader<User>(Env.ConnectionString, "_ts");

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
            var conn = new SqlConnection(Env.ConnectionString);
            var reader = new SqlContinuousTimestampReader<User>(Env.ConnectionString, "_ts");

            // Act
            bool read = false;
            reader.Start();
            await reader.WhenInitialReadFinished();

            reader.NewValue += (key, user) => read = true;

            conn.Execute(
                @"INSERT INTO [User] (FirstName, LastName, Age, Gender)
                          VALUES (@firstName, @lastName, @age, @gender);",
                Env.UserFaker.Generate());

            await Task.Delay(400); // doubled default delay (200ms)

            // Assert
            Assert.IsTrue(read);
        }

        [Test]
        public async Task Should_read_updated_data()
        {
            // Arrange
            var conn = new SqlConnection(Env.ConnectionString);
            var reader = new SqlContinuousTimestampReader<User>(Env.ConnectionString, "_ts");

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

            conn.Execute(
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
            var conn = new SqlConnection(Env.ConnectionString);
            var reader = new SqlContinuousTimestampReader<User2>(Env.ConnectionString, "_ts");

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
            Assert.That(() => new SqlContinuousTimestampReader<User>(Env.ConnectionString, timestampColumnName: null),
                Throws.ArgumentNullException);
        }
    }
}
