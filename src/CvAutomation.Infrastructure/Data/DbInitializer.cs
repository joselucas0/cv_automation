using System;
using System.Threading.Tasks;

namespace CvAutomation.Infrastructure.Data;

public static class DbInitializer
{
    public static Task SeedAsync(IServiceProvider serviceProvider)
    {
        // O seeding agora é totalmente gerenciado pelo script de automação Python 'seed_database.py'
        Console.WriteLine("DbInitializer: Banco de dados inicializado. O seeding é gerenciado via script Python.");
        return Task.CompletedTask;
    }
}
