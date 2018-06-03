namespace InMemoryDb.Tests
{
    public class User4
    {
        [RowKey]
        public string FirstName { get; set; }
        [RowKey]
        public string LastName { get; set; }
        public int Age { get; set; }
        public bool Gender { get; set; }
    }
}
