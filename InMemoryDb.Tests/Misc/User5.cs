namespace InMemoryDb.Tests
{
    [Table("User")]
    public class User5
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public bool Gender { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
