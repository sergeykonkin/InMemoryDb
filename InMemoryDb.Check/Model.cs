using System.ComponentModel.DataAnnotations.Schema;

namespace InMemoryDb.Check
{
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

        protected bool Equals(UserGroup other)
        {
            return UserId == other.UserId && GroupId == other.GroupId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UserGroup) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (UserId * 397) ^ GroupId;
            }
        }
    }

    public class Group
    {
        public int Id;
        public string Name;
    }
}
