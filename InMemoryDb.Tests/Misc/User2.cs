using System.ComponentModel.DataAnnotations.Schema;

namespace InMemoryDb.Tests
{
    [Table("User")]
    public class User2
    {
        [Column("Id")]
        public int Id2 { get; set; }

        [Column("FirstName")]
        public string FirstName2 { get; set; }

        [Column("LastName")]
        public string LastName2 { get; set; }

        [Column("Age")]
        public int Age2 { get; set; }

        [Column("Gender")]
        public bool Gender2 { get; set; }
    }
}
