using LibraryManagementSystem.Data;

namespace LibraryManagementSystem.Data.Seed
{
    internal static class DbInitializerExtension
    {
        public static IApplicationBuilder UseItToSeedSqlServer(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app, nameof(app));

            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<LibraryContext>();
                DbInitializer.Initialize(context);
            }
            catch (Exception ex)
            {
                // Log exception if needed
                Console.WriteLine($"Erro ao fazer seed da base de dados: {ex.Message}");
            }

            return app;
        }
    }
}
