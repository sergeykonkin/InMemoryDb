using System.Data.SqlClient;
using Bogus;
using Bogus.DataSets;
using Dapper;
using NUnit.Framework;
using RimDev.Automation.Sql;

namespace InMemoryDb.Tests
{
    [SetUpFixture]
    public class Setup
    {
        private LocalDb _db = null;

        [OneTimeSetUp]
        public void BeforeAllTests()
        {
            InitDb();
            FillDb();
        }

        [OneTimeTearDown]
        public void AfterAllTest()
        {
            Cleanup();
            _db?.Dispose();
        }

        private void InitDb()
        {
#if DEBUG
            _db = new LocalDb(version: "mssqllocaldb");
            Env.ConnectionString = _db.ConnectionString;
            var conn = _db.OpenConnection();
#else
            var appVeyorConnectionString = "Server=(local)\\SQL2017;Database=master;User ID=sa;Password=Password12!";
            Env.ConnectionString = appVeyorConnectionString;
            var conn = new SqlConnection(appVeyorConnectionString);
#endif
            using (conn)
            {
                conn.Execute(@"
CREATE TABLE [User] (
    Id int PRIMARY KEY IDENTITY(1, 1),
    FirstName nvarchar(250),
    LastName nvarchar(250),
    Age int,
    Gender bit,
    [_ts] rowversion
)");
            }
        }

        private void FillDb()
        {
            var userFaker = new Faker<User>()
                .RuleFor(u => u.FirstName, f => f.Person.FirstName)
                .RuleFor(u => u.LastName, f => f.Person.LastName)
                .RuleFor(u => u.Age, f => f.Person.Random.Number(18, 60))
                .RuleFor(u => u.Gender, f => f.Person.Gender == Name.Gender.Male);

            Env.UserFaker = userFaker;

            for (int i = 0; i < 12345; i++)
            {
                using (var conn = new SqlConnection(Env.ConnectionString))
                {
                    conn.Execute(
                        @"INSERT INTO [User] (FirstName, LastName, Age, Gender)
                          VALUES (@firstName, @lastName, @age, @gender);",
                        userFaker.Generate());
                }
            }
        }

        private void Cleanup()
        {
            using (var conn = new SqlConnection(Env.ConnectionString))
            {
                conn.Execute("DROP TABLE [User]");
            }
        }
    }
}
