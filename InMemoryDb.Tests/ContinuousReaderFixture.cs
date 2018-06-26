using System;
using System.Data.SqlClient;
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
            var reader = new ContinuousReader<User>(new SqlTimestampReader<User>(Env.ConnectionString));

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
            var reader = new ContinuousReader<User>(new SqlTimestampReader<User>(Env.ConnectionString));

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
            var reader = new ContinuousReader<User>(new SqlTimestampReader<User>(Env.ConnectionString));

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
                  WHERE Id = 1337");

            await Task.Delay(400); // doubled default delay (200ms)

            // Assert
            Assert.IsTrue(read);
            Assert.AreEqual(1337, id);
            Assert.AreEqual("Sereja", newName);
        }

        [Test]
        public async Task Should_read_deleted_data()
        {
            // Arrange
            var reader = new ContinuousReader<User>(new SqlTimestampReader<User>(Env.ConnectionString));

            // Act
            bool newValueRead = false;
            bool deletedValueRead = false;
            int deletedId = 0;
            reader.Start();
            await reader.WhenInitialReadFinished();

            reader.NewValue += (key, user) =>
            {
                newValueRead = true;
            };

            reader.DeletedValue += (key, user) =>
            {
                deletedValueRead = true;
                deletedId = user.Id;
            };

            new SqlConnection(Env.ConnectionString).Execute(
                @"UPDATE [User]
                  SET Deleted = 1
                  WHERE Id = 5555");

            await Task.Delay(400); // doubled default delay (200ms)

            // Assert
            Assert.IsFalse(newValueRead);
            Assert.IsTrue(deletedValueRead);
            Assert.AreEqual(5555, deletedId);
        }

        [Test]
        public void Should_throw_on_bad_arguments()
        {
            Assert.That(() => new ContinuousReader<User>(null), Throws.ArgumentNullException);
            Assert.That(() => new ContinuousReader<User>(new SqlTimestampReader<User>(Env.ConnectionString), delay: 0),
                Throws.Exception.TypeOf(typeof(ArgumentOutOfRangeException)));
        }
    }
}
