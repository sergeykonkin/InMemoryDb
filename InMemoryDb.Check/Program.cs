using System;
using System.Linq;

namespace InMemoryDb.Check
{
    public class Program
    {
        public static void Main()
        {
            Setup.Init();

            var db = new MyDatabase();
            db.Setup(handleException: Console.WriteLine);
            db.Init();

            var admins =
                (from u in db.Users
                join ug in db.UserGroups on u.Id equals ug.UserId
                join g in db.Groups on ug.GroupId equals g.Id
                where g.Name == "Admin"
                select u).ToList();

            var ok = admins.Single() == db.Users[777];
        }
    }

    public class MyDatabase : Database
    {
        public Table<User> Users { get; }
        public Table<Group> Groups { get; }
        public Table<UserGroup> UserGroups { get; }

        public MyDatabase() : base(Check.Setup.LocalDb.ConnectionString)
        {
            Users = Table<User>();
            Groups = Table<Group>();
            UserGroups = Table<UserGroup>();
        }
    }
}
