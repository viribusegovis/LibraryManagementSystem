using LibraryManagementSystem.Data;

namespace LibraryManagementSystem.Data.Seed
{
    /**
     * Extensão para inicialização automática da base de dados durante o arranque da aplicação
     * Implementa padrão de extensão para IApplicationBuilder permitindo configuração fluente
     * Garante que dados de teste são criados em ambiente de desenvolvimento
     */
    internal static class DbInitializerExtension
    {
        /**
         * Executa inicialização da base de dados com dados de teste
         * 
         * @param app Builder da aplicação ASP.NET Core
         * @return O mesmo builder para permitir encadeamento de métodos
         * @throws ArgumentNullException Se o builder da aplicação for nulo
         * 
         * Este método implementa os seguintes requisitos obrigatórios:
         * - Acesso à base de dados através do EntityFramework
         * - Criação de pelo menos três tabelas (Books, Members, Categories, etc.)
         * - Relacionamentos "muitos-para-um" e "muitos-para-muitos"
         * - Controlo de acesso com dois tipos de utilizadores (Bibliotecário/Membro)
         * - Credenciais de acesso iniciais para teste
         */
        public static IApplicationBuilder UseItToSeedSqlServer(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app, nameof(app));

            /* Criar scope para acesso aos serviços registados */
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                /* Obter contexto da base de dados através de injeção de dependências */
                var context = services.GetRequiredService<LibraryContext>();

                /* Executar inicialização assíncrona de forma síncrona
                   Necessário porque este método de extensão não pode ser async */
                Task.Run(async () => await DbInitializer.Initialize(context)).Wait();

                Console.WriteLine("Base de dados inicializada com dados de teste com sucesso.");
            }
            catch (Exception ex)
            {
                /* Tratamento de erros conforme requisitos: "mensagens de erro adequadas" */
                Console.WriteLine($"Erro ao fazer seed da base de dados: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                /* Em ambiente de desenvolvimento, relançar exceção para debugging */
#if DEBUG
                throw;
#endif
            }

            return app;
        }
    }
}
