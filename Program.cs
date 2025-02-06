using Be_QuanLyKhoaHoc.Extensions;
using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Identity.Entities;
using Be_QuanLyKhoaHoc.Services;
using Be_QuanLyKhoaHoc.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
// Thêm dịch vụ Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Cấu hình tùy chọn cho Identity (có thể tùy chỉnh theo yêu cầu)
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 8;

    options.SignIn.RequireConfirmedEmail = true;

    options.User.RequireUniqueEmail = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGenWithAuth();

builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<LoginUser>();
builder.Services.AddScoped<IAppEmailSender, AppEmailSender>();

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireRole("Admin", "Lecturer", "User")
        .Build();

});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,  // Kiểm tra Issuer để đảm bảo token đến từ nguồn tin cậy
            ValidateAudience = true,  // Kiểm tra Audience để token chỉ phục vụ đúng client
            ValidateLifetime = true,  // Bắt buộc kiểm tra thời gian hết hạn của token
            ValidateIssuerSigningKey = true,  // Kiểm tra khóa bí mật có hợp lệ không

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? string.Empty)),
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();
app.UseCors("AllowSpecificOrigin");

// Configure the HTTP request pipeline.

app.UseRouting();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}


app.UseHttpsRedirection();

// Middleware cho xác thực và phân quyền
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
