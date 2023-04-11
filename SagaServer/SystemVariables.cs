using System;
using System.IO;
using RandomStringGenerator;
using SagaDb.Databases;
using SagaDb.Models;

namespace SagaUtil;

public class SystemVariables
{
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

    public static SystemVariables Instance { get; } = new();

    public void UpdateState(string userDb, string bookDb, string signingKey)
    {
        BookDb = bookDb;
        UserDb = userDb;
        JwtSigningKey = signingKey;

        if (!File.Exists(UserDb))
        {
            // Need to create a userdb for first run with admin account
            var _userCommands = new UserCommands(UserDb);
            var _user = new User();
            _user.FullName = "Admin";
            _user.UserName = "admin";
            _user.Password = "admin";
            _user.UserRole = "Admin";
            _user.PasswordSalt = StringGenerator.GetUniqueString(32);

            _userCommands.InsertUser(_user);
            Console.WriteLine("UserDb created, admin user with password admin created. Change this password!");
        }
    }
}