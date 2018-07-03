using System;

namespace InMemoryDb.Check
{
    public class Program
    {
        public static void Main()
        {
            Setup.Init();

            var table = new InMemoryTable<int, User>(Setup.LocalDb.ConnectionString, user => user.UserId);
            table.Start(handleException: Console.WriteLine);
            Console.ReadLine();

            Setup.LocalDb.Dispose();
        }
    }
}
