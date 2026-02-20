using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class venuesSeedForTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlBuilder = new StringBuilder();

            //Generate some ACCENTED and equivalent NO-ACCENTED entries for testing
            var accentNames = new[]
            {
                "Café Central",
                "São Paulo Bistro",
                "Japão Grill",
                "Cafe Bistro"
            };

            for (int i = 0; i < accentNames.Length; i++)
            {
                sqlBuilder.AppendLine($"INSERT INTO Venues (Name) VALUES ('{accentNames[i]}');");
            }

            // Replace simple sequential names with randomized venue names for better test coverage
            {
                // word lists (include many Portuguese accented entries)
                var adjectives = new[]
                {
                    "Azul", "Dourado", "Velho", "Bom", "Doce", "Feliz", "Sol", "Verde",
                    "Maravilha", "Encantado", "Café", "São", "Málaga", "Björk",
                    "Belo", "Quente", "Frio", "Sagrado", "Querido", "Saudade"
                };

                var nouns = new[]
                {
                    "Praça", "Porto", "Rio", "Mar", "Sol", "Coração", "Açúcar", "Pão",
                    "Cantinho", "Canto", "Baía", "Praia", "Bistrô", "Pastelaria", "Adega",
                    "Tasca", "Mercado", "Jardim", "Fado", "Comércio"
                };

                var types = new[] { "", "Bar", "Lounge", "Casa", "Inn", "Restaurant", "Cantina", "Taberna", "Mercearia", "Pastelaria" };

                var portugueseFixed = new[]
                {
                    "Café do Porto",
                    "Casa de São João",
                    "Cantinho da Saudade",
                    "Praça do Comércio",
                    "Mercado do Pão"
                };

                var rnd = new System.Random();

                for (int i = 1; i <= 500; i++)
                {
                    // occasionally pick a fully Portuguese fixed name
                    if (rnd.NextDouble() < 0.08)
                    {
                        var fixedName = portugueseFixed[rnd.Next(portugueseFixed.Length)];
                        var escapedFixed = fixedName.Replace("'", "''");
                        sqlBuilder.AppendLine($"INSERT INTO Venues (Name) VALUES ('{escapedFixed}');");
                        continue;
                    }

                    var adj = adjectives[rnd.Next(adjectives.Length)];
                    var noun = nouns[rnd.Next(nouns.Length)];
                    var type = types[rnd.Next(types.Length)];

                    string name;
                    switch (rnd.Next(6))
                    {
                        case 0:
                            name = $"{adj} {noun}";
                            break;
                        case 1:
                            name = $"{adj} {noun} {type}".Trim();
                            break;
                        case 2:
                            name = $"{noun} {i}";
                            break;
                        case 3:
                            name = $"{adj}'s {noun}";
                            break;
                        case 4:
                            name = $"Casa do {noun}";
                            break;
                        default:
                            name = $"{noun} de {adj}";
                            break;
                    }

                    // ensure proper SQL escaping for single quotes
                    var escapedName = name.Replace("'", "''");
                    sqlBuilder.AppendLine($"INSERT INTO Venues (Name) VALUES ('{escapedName}');");
                }
            }

            migrationBuilder.Sql(sqlBuilder.ToString());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Venues");
        }
    }
}
