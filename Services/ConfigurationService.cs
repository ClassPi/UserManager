namespace UserManager.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public IConfiguration Configuration { get; }

        public ConfigurationService()
        {
            Configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory()) // 设置配置文件所在的目录
                                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // 加载配置文件
                                .Build();
        }
    }

    public interface IConfigurationService
    {
        public IConfiguration Configuration { get; }
    }

    public class ConfigurationNotExistsException : Exception
    {
        public ConfigurationNotExistsException(string message = "未找到对应键值对，请检查appsetting.json是否正确") : base(message)
        {
        }
    }
}
