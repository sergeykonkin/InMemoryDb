using System.Data.SqlClient;
using Bogus;
using Bogus.DataSets;
using Dapper;
using RimDev.Automation.Sql;

namespace InMemoryDb.Check
{
    public static class Setup
    {
        public static LocalDb LocalDb;

        public static void Init()
        {
            CreateDb();
            FillWithTestData();
        }

        private static void CreateDb()
        {
            LocalDb = new LocalDb(version: "mssqllocaldb");

            using (var conn = new SqlConnection(LocalDb.ConnectionString))
            {
                conn.Execute(@"
CREATE TABLE [User] (
    Id int PRIMARY KEY IDENTITY(1, 1),
    FirstName nvarchar(250),
    LastName nvarchar(250),
    Age int,
    Gender bit,
    RowVersion rowversion,
    IsDeleted bit not null default(0)
)");
            }
        }

        private static void FillWithTestData()
        {
            var userFaker = new Faker<User>()
                .RuleFor(u => u.FirstName, f => f.Person.FirstName)
                .RuleFor(u => u.LastName, f => f.Person.LastName)
                .RuleFor(u => u.Age, f => f.Person.Random.Number(18, 60))
                .RuleFor(u => u.Gender, f => f.Person.Gender == Name.Gender.Male);

            for (int i = 0; i < 1234; i++)
            {
                using (var conn = new SqlConnection(LocalDb.ConnectionString))
                {
                    var user = userFaker.Generate();
                    conn.Execute(
                        @"INSERT INTO [User] (FirstName, LastName, Age, Gender)
                          VALUES (@firstName, @lastName, @age, @gender);",
                        new {user.FirstName, user.LastName, user.Age, user.Gender});
                }
            }
        }
    }
}
