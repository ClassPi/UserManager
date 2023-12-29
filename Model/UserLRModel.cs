using System.ComponentModel.DataAnnotations;
namespace UserManager.Model;
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
public class UserLoginModel
{
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    [Required]
    public bool Remember { get; set; } = false;
    /*
    [Required]
    public string Captcha_token { get; set; }
    */
}
public class UserRegisterModel
{

    [Required(ErrorMessage = "名称不能为空")]
    public string Name { get; set; }

    [Required(ErrorMessage = "电子邮件不能为空")]
    [DataType(DataType.EmailAddress)]
    [RegularExpression(@"^(.+)@(.+)\.(com|net|org|cn)$", ErrorMessage = "不符合规范的邮箱格式")]
    [EmailAddress(ErrorMessage = "电子邮件格式不正确")]
    public string Email { get; set; }

    [Required(ErrorMessage = "密码不能为空")]
    [DataType(DataType.Password)]
    [StringLength(20, MinimumLength = 6, ErrorMessage = "密码长度必须在6到20之间")]
    public string Password { get; set; }

    [Required(ErrorMessage = "确认密码不能为空")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
    public string Repassword { get; set; }

    /*[Required]
    public string Captcha_token { get; set; }
    */
}
