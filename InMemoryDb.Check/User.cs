using System.ComponentModel.DataAnnotations.Schema;

namespace InMemoryDb.Check
{
    public class User
    {
        [Column("Id")] public int UserId;
        public string FirstName;
        public string LastName;
        public int Age;
        public bool Gender;

        public string FullName => $"{FirstName} {LastName}";
    }
}
