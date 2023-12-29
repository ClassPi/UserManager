using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace UserManager.Database
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<UserDbModel> Users { get; set; }


        public async Task<UserDbModel?> GetUser(Expression<Func<UserDbModel, bool>> predicate)
        {
            // 使用LINQ查询从数据库中查找匹配的用户
            var user = await Users.FirstOrDefaultAsync(predicate);
            // 返回用户对象
            return user;
        }

        public async Task<bool> CreateUser(UserDbModel user)
        {
            // 使用LINQ查询从数据库中查找匹配的用户
            var userExist = await Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            // 如果用户已存在，则返回 false
            if (userExist != null)
                return false;
            else
            {
                await Users.AddAsync(user);
                await SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> AuthUser(string email, string password)
        {
            // 使用LINQ查询从数据库中查找匹配的用户
            var tar = await Users.FirstOrDefaultAsync(u => u.Email == email);
            if (tar is null)
                return false;
            bool result = BCrypt.Net.BCrypt.Verify(password, tar.Password);
            return result;
        }

        public async Task<bool> UpdateUser(UserDbModel user)
        {
            // 使用LINQ查询从数据库中查找匹配的用户//
            //修改`tar`对象的属性会影响到数据库中的数据。在 Entity Framework 中，当你从数据库查询出一个对象，这个对象会被上下文跟踪。
            //这意味着，当你修改这个对象的属性并调用`SaveChangesAsync`方法时，
            //Entity Framework 会生成相应的 SQL 更新语句来更新数据库中的数据。
            //在这段代码中，当你修改`tar.IsActivated`和`tar.Isadmin`的值并调用`SaveChangesAsync`方法后，这些更改会被保存到数据库中。
            var tar = await Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (tar is null)
                return false;
            tar.IsActivated = user.IsActivated;
            tar.Isadmin = user.Isadmin;
            await SaveChangesAsync();
            return true;
        }
    }


}
