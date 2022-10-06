using SagaDb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SagaUtil;

namespace SagaDb.Databases
{
    public class UserCommands
    {
        private readonly string _dbFile;
        private bool _isEnsuredCreated;

        public UserCommands()
        {
            _isEnsuredCreated = false;
            //this._dbFile = @"D:\Testing\user-test.db";
            this._dbFile = @"user-test.db";
            using var db = new UserContext(this._dbFile);
            db.Database.EnsureCreated();
            _isEnsuredCreated = true;
        }

        public UserCommands(string dbFile)
        {
            _isEnsuredCreated = false;
            this._dbFile = dbFile;
            using var db = new UserContext(this._dbFile);
            db.Database.EnsureCreated();
            _isEnsuredCreated = true;
        }

        public UserContext InitContext(UserContext context)
        {
            if (context == null)
            {
                context = new UserContext(this._dbFile);
                context.ChangeTracker.AutoDetectChangesEnabled = false;

                if (!_isEnsuredCreated)
                {
                    context.Database.EnsureCreated();
                    _isEnsuredCreated = true;
                }
            }
            return context;
        }

        #region Users

        public User GetUser(string username, UserContext db = null)
        {
            db = InitContext(db);

            return db.Users
                .FirstOrDefault(u => u.UserName == username);
        }

        public void InsertUser(User user, UserContext db = null)
        {
            db = InitContext(db);

            user.Password = UserHelper.GenerateSaltedHash(user.Password, user.PasswordSalt);

            db.Users.Add(user);
            db.SaveChanges();
        }

        public void UpdateUser(User user, UserContext db = null)
        {
            db = InitContext(db);

            user.Password = UserHelper.GenerateSaltedHash(user.Password, user.PasswordSalt);

            db.Users.Attach(user);
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();
        }

        public User GetUserByUsername(string userName, UserContext db = null)
        {
            db = InitContext(db);

            return db.Users.Where(u => u.UserName == userName).FirstOrDefault();
        }

        #endregion

        #region Progress

        public BookProgress GetProgress(int Id, UserContext db = null)
        {
            db = InitContext(db);

            return db.BookProgresses
                .FirstOrDefault(p => p.Id == Id);
        }

        public RefreshToken GetUserByRefreshToken(string refreshTokenValue, UserContext db = null)
        {
            db = InitContext(db);

            return db.RefreshTokens.Where(r => r.RefreshTokenValue == refreshTokenValue).FirstOrDefault();
        }

        public void InsertProgress(BookProgress progress, UserContext db = null)
        {
            db = InitContext(db);

            db.BookProgresses.Add(progress);
            db.SaveChanges();
        }

        public void UpdateProgress(BookProgress progress, UserContext db = null)
        {
            db = InitContext(db);

            db.BookProgresses.Attach(progress);
            db.Entry(progress).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void DeleteProgress(BookProgress progress, UserContext db = null)
        {
            db = InitContext(db);
            db.BookProgresses.Attach(progress);
            db.Entry(progress).State = EntityState.Deleted;
            db.SaveChanges();
        }

        #endregion

        #region Refresh Tokens

        public RefreshToken GetRefreshToken(string refreshToken, UserContext db = null)
        {
            db = InitContext(db);

            return db.RefreshTokens
                .FirstOrDefault(r => r.RefreshTokenValue == refreshToken);
        }

        public RefreshToken GetRefreshToken(int tokenId, UserContext db = null)
        {
            db = InitContext(db);

            return db.RefreshTokens
                .FirstOrDefault(r => r.Id == tokenId);
        }

        public void InsertRefreshToken(RefreshToken refreshToken, UserContext db = null)
        {
            db = InitContext(db);

            db.RefreshTokens.Add(refreshToken);
            db.SaveChanges();
        }

        public void UpdateRefreshToken(RefreshToken refreshToken, UserContext db = null)
        {
            db = InitContext(db);

            db.RefreshTokens.Attach(refreshToken);
            db.Entry(refreshToken).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void DeleteRefreshToken(RefreshToken refreshToken, UserContext db = null)
        {
            db = InitContext(db);
            db.RefreshTokens.Attach(refreshToken);
            db.Entry(refreshToken).State = EntityState.Deleted;
            db.SaveChanges();
        }
        #endregion
    }
}
