namespace InMemoryDb.Tests
{
    [OriginName("User")]
    public class User6
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public bool Gender { get; set; }

        [Ignore]
        public string FullName => $"{FirstName} {LastName}";
    }
}
