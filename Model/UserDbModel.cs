using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// 此为EF用户模型, 不必担心实体化数据库记录时的值初始化问题，因为EF会使用代理类来实现实体化，不使用构造函数。
/// </summary>
[Table("Users")]
public class UserDbModel
{
    public UserDbModel(string username, string password, string email)
    {
        Username = username;
        Password = password;
        Email = email;
    }

    [Key]
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required]
    public string Username { get; set; }
    [Required]
    [PasswordPropertyText]
    public string Password { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    public bool Isadmin { get; set; } = false;
    [Required]
    public bool IsActivated { get; set; } = false;

}