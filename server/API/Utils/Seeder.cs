using Infra;
using LinqToDB;

namespace API.Utils;

public class Seeder(MyDatabaseConnection db)
{
    public void Seed()
    {
        db.CreateTable<User>(tableOptions: TableOptions.CreateIfNotExists);
    }
}