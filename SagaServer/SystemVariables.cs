using SagaDb.Databases;
using SagaDb.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SagaUtil
{
    public class SystemVariables
    {
        private static readonly SystemVariables instance = new SystemVariables();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static SystemVariables()
        {
        }

        private SystemVariables()
        {
        }

        public string BookDb { get; private set; }
        public string UserDb { get; private set; }
        public string JwtSigningKey { get; private set; }
        public string JwtIssuer => "SagaServer";
        public string JwtAudience => "SagaServer";
        public string Protocol => "http";

        public void UpdateState(string userDb, string bookDb, string signingKey)
        {
            this.BookDb = bookDb;
            this.UserDb = userDb;
            this.JwtSigningKey = signingKey;

            if (!File.Exists(this.UserDb))
            {
                // Need to create a userdb for first run with admin account
                var _userCommands = new UserCommands(this.UserDb);
                var _user = new User();
                _user.FullName = "Admin";
                _user.UserName = "admin";
                _user.Password = "admin";
                _user.UserRole = "Admin";
                _user.PasswordSalt = RandomStringGenerator.StringGenerator.GetUniqueString(32);

                _userCommands.InsertUser(_user);
                Console.WriteLine("UserDb created, admin user with password admin created. Change this password!");
            }

        }

        public static SystemVariables Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
