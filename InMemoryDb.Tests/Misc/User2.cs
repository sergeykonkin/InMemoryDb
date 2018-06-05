namespace InMemoryDb.Tests
{
    [OriginName("User")]
    public class User2
    {
        [OriginName("Id")]
        [RowKey]
        public int Id2 { get; set; }

        [OriginName("FirstName")]
        public string FirstName2 { get; set; }

        [OriginName("LastName")]
        public string LastName2 { get; set; }

        [OriginName("Age")]
        public int Age2 { get; set; }

        [OriginName("Gender")]
        public bool Gender2 { get; set; }
    }
}
