using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.Text;
using UserManager.Database;
using UserManager.Services;

namespace UserManager;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(2685); // HTTP
        });
        // Add services to the container.
        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddDbContext<UserDbContext>(options =>
        {
            var connectionString = new MySqlConnectionStringBuilder
            {
                Server = builder.Configuration["SQL:Server"],
                Database = builder.Configuration["SQL:Database"],
                UserID = builder.Configuration["SQL:UserID"],
                Password = builder.Configuration["SQL:Password"],
                Port = uint.Parse(builder.Configuration["SQL:Port"]!)
            }.ConnectionString;

            options.UseMySql(connectionString, ServerVersion.Parse("8.2.0"));
        });

        //���ÿ�������������web�˵ĵ�¼����
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                var allowOrigins = builder.Configuration.GetSection("CORS:AllowOrigins").Get<string[]>();
                policy.WithOrigins(allowOrigins!)
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();                       
            });


        });

        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    // �Ƿ���֤�䷢��
                    ValidateIssuer = true,
                    // �Ƿ���֤����Ⱥ��
                    ValidateAudience = true,
                    // �Ƿ���֤������
                    ValidateLifetime = true,
                    // ��֤Token��ʱ��ƫ����
                    ClockSkew = TimeSpan.FromSeconds(30),
                    // ��֤�䷢��
                    ValidIssuer = builder.Configuration["JWT:Issuer"],
                    // ��֤����Ⱥ��
                    ValidAudiences = builder.Configuration["JWT:Audience"]?.Split(','),
                    // ��֤��Կ
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Token"]!))
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var isExists = context.Request.Headers.TryGetValue("Authorization", out var token);
                        if (!isExists)
                            return Task.CompletedTask;
                        var tokenString = token.ToString();
                        if (tokenString.StartsWith("Bearer"))
                            context.Token = tokenString[6..];
                        return Task.CompletedTask;
                    }
                };
            });

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetService<UserDbContext>()!;
            var logger = scope.ServiceProvider.GetService<ILogger<Program>>()!;
            try
            {
                db.Database.EnsureCreated();
            }
            catch (MySqlException e)
            {
                logger.LogError("�������ݿ�ʧ��,����:\n");
                logger.LogError(e.Message + "\n" + "      ��ȡ���������ļ�Ϊ:\n" +
                                                    $"      Server: {builder.Configuration["SQL:Server"]}\n" +
                                                    $"      Database: {builder.Configuration["SQL:Database"]}\n" +
                                                    $"      UserID: {builder.Configuration["SQL:UserID"]}\n" +
                                                    $"      Password: {builder.Configuration["SQL:Password"]}\n" +
                                                    $"      Port: {builder.Configuration["SQL:Port"]}\n");
                return;
            }

        }

        app.UseCors("AllowAll");

        app.UseHttpsRedirection();

        app.UseAuthorization();
        app.UseAuthentication();

        app.MapControllers();

        app.Run();
    }
}

