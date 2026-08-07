
using CacheBusterMicroservice.Utilities;

namespace CacheBusterMicroservice
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			// 1. Fetch the connection string from appsettings.json
			string connectionString = builder.Configuration.GetConnectionString("VM-SQL")
				?? throw new InvalidOperationException("Connection string 'VM-SQL' not found.");

			// 2. Register the context as a Singleton
			builder.Services.AddSingleton(new DatabaseContext(connectionString));

			builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

			// 1. Enable default file mapping (looks for index.html)
			app.UseDefaultFiles();

			// 2. Enable serving static files from wwwroot
			app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI();
            
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

			app.Run();
        }
    }
}
