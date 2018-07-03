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

            Console.ReadLine();
        }
    }

    public class MyDatabase : InMemoryDatabase
    {
        public InMemoryTable<User> Users { get; }
        public InMemoryTable<Group> Groups { get; }
        public InMemoryTable<UserGroup> UserGroups { get; }

        public MyDatabase() : base(Check.Setup.LocalDb.ConnectionString)
        {
            Users = Table<User>(user => user.Id);
            Groups = Table<Group>(group => group.Id);
            UserGroups = Table<UserGroup>();
        }
    }
}
