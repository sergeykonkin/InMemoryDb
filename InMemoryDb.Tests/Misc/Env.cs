using Bogus;

namespace InMemoryDb.Tests
{
    public static class Env
    {
        public static string ConnectionString { get; set; }
        public static Faker<User> UserFaker { get; set; }
    }
}
