﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InMemoryDb.Check
{
    [Table("User", Schema = "dbo")]
    public class User
    {
        public int Id;
        public string FirstName;
        public string LastName;
        public int Age;
        public bool Gender;

        public string FullName => $"{FirstName} {LastName}";
    }

    public class UserGroup
    {
        public int UserId;
        public int GroupId;
    }

    public class Group
    {
        public int Id;
        public string Name;
    }
}
