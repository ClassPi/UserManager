using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using UserManager.Database;
using BCrypt.Net;
using UserManager.Services;
using UserManager.Tools;
using System.Security.Claims;
using UserManager.Model;


namespace UserManager.Controllers
{
    [ApiController]
    [Route("user")]
    public class AuthController : ControllerBase
    {
        private readonly IConfigurationService _configuration;
        private readonly UserDbContext _userDbContext;

        //定义一个常量，表示登录失败次数的上限
        const int MAX_FAIL_COUNT = 5;
        //定义一个字典，用于存储IP地址和登录失败次数的映射
        static readonly Dictionary<string, int> ipFailCount = new Dictionary<string, int>();
        //定义一个集合，用于存储被列入黑名单的IP地址
        static readonly HashSet<string> ipBlackList = new HashSet<string>();
        public AuthController(UserDbContext dBTool, IConfigurationService configuration)
        {
            _userDbContext = dBTool;
            _configuration = configuration;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginModel lg)
        {
            // 验证模型是否合法
            if (ModelState.IsValid == false)
                return new JsonResult(new { success = false, message = "格式验证失败" });
            var user = await _userDbContext.GetUser(x => x.Email == lg.Email);
            //判断查询结果
            if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(lg.Password, user.Password, HashType.SHA512))
            {
                //获取当前用户的IP地址
                string ip = HttpContext.Connection.RemoteIpAddress!.ToString();
                //判断当前用户的IP地址是否在黑名单中
                if (ipBlackList.Contains(ip))
                    return new JsonResult(new { success = false, message = "您最近的重试次数过多" });
                //如果登录失败，将当前用户的IP地址和失败次数加入字典中，或者更新失败次数
                if (ipFailCount.TryGetValue(ip, out int ipvalue))
                    ipFailCount[ip] = ++ipvalue;
                else
                    ipFailCount.Add(ip, 1);
                //如果失败次数超过上限，将当前用户的IP地址加入黑名单中
                if (ipFailCount[ip] > MAX_FAIL_COUNT)
                {
                    ipBlackList.Add(ip);
                    return new JsonResult(new { success = false, message = "重试次数过多，请稍后再试。" });
                }
                //计算剩余的尝试机会
                int remain = MAX_FAIL_COUNT - ipFailCount[ip];
                //提示用户还有几次尝试机会
                return new JsonResult(new { success = false, message = $"用户名或密码错误，您还有{remain}次尝试机会。" });
            }
            else
            {
                //如果登录成功，将当前用户的IP地址从字典中移除
                string ip = HttpContext.Connection.RemoteIpAddress!.ToString();
                ipFailCount.Remove(ip);
                //生成Token
                var token = GenerateToken(user);
                //为客户端设置cookie
                Response.Cookies.Append("Authorization", token, new CookieOptions()
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.Now.AddDays(1),
                    SameSite = SameSiteMode.None,
                    Secure = false
                });
                return new JsonResult(new { success = true, token = token });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterModel rg)
        {
            if (ModelState.IsValid == false)
            {
                //获取所有错误的Key
                List<string> Keys = [.. ModelState.Keys];
                var errors = new List<ModelError>();
                //获取每一个key对应的ModelStateDictionary
                foreach (var key in Keys)
                {
                    errors = [.. ModelState[key]!.Errors];
                }
                return new JsonResult(new { success = false, message = errors[0].ErrorMessage });
            }

            var user = await _userDbContext.GetUser(u => u.Email == rg.Email);

            if (user != null)
                return new JsonResult(new { success = false, message = "该邮箱已被注册" });
            else
            {
                var passwordHashString = BCrypt.Net.BCrypt.EnhancedHashPassword(rg.Password, HashType.SHA512, 10);

                var newUser = new UserDbModel(rg.Name, passwordHashString, rg.Email);

                await _userDbContext.CreateUser(newUser);

                return new JsonResult(new { success = true });
            }

        }
        public string GenerateToken(UserDbModel user)
        {
            // 创建一个令牌处理器
            var handler = new JwtSecurityTokenHandler();
            // 创建一个令牌描述符，包含令牌的头部、有效载荷和签名
            var descriptor = new SecurityTokenDescriptor()
            {
                // 设置令牌的类型和算法
                TokenType = "JWT",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.Configuration["JWT:Token"] ?? throw new ConfigurationNotExistsException())),
                    SecurityAlgorithms.HmacSha256
                ),
                // 设置令牌的颁发者、受众、过期时间等
                Issuer = _configuration.Configuration["JWT:Issuer"] ?? throw new ConfigurationNotExistsException(),
                Audience = _configuration.Configuration.GetSection("JWT:Audience").GetChildren().ConvertToString() ?? throw new ConfigurationNotExistsException(),
                Expires = DateTime.UtcNow.AddMonths(1),
                // 设置令牌的数据和声明
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, "user"),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                })
            };
            // 使用令牌处理器和令牌描述符创建一个令牌
            var token = handler.CreateToken(descriptor);
            // 使用令牌处理器将令牌转换为字符串
            var tokenString = handler.WriteToken(token);
            tokenString = "Bearer" + tokenString;
            return tokenString;
        }

        /*static async Task<bool> AuthCaptcha(string token)
        {
            //加载配置文件
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

            //添加配置文件路径
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var configuration = configurationBuilder.Build();

            var secret = configuration["Recaptcha:Secret"];

            if (secret == null)
            {
                throw new Exception("Recaptcha:Secret is null");
            }
            //Verify the recaptcha
            var result = await RecaptchaHelper.VerifyRecaptchaAsync(token, secret);

            if (result == false)
            {
                return false;
            }

            return true;
        }*/

    }
}











