
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace WypozyczalniaSzkolna.Data
{
    public static class BazaDanych
    {
        private static readonly string FolderBazy =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "WypozyczalniaSzkolna");

        private static readonly string SciezkaBazy =
            Path.Combine(FolderBazy, "wypozyczalnia.db");

        private static string Polaczenie =>
            $"Data Source={SciezkaBazy}";

        public static void Inicjalizuj()
        {
            Directory.CreateDirectory(FolderBazy);

            using var polaczenie =
                new SqliteConnection(Polaczenie);

            polaczenie.Open();

            using var polecenie = polaczenie.CreateCommand();

            polecenie.CommandText = """
                PRAGMA foreign_keys = ON;

                CREATE TABLE IF NOT EXISTS Sprzet (
                    Id TEXT PRIMARY KEY,
                    Nazwa TEXT NOT NULL,
                    Typ TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Wypozyczenie (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IdSprzetu TEXT NOT NULL,
                    Osoba TEXT NOT NULL,
                    DataWypozyczenia TEXT NOT NULL,
                    DataZwrotu TEXT NULL,
                    FOREIGN KEY (IdSprzetu)
                        REFERENCES Sprzet(Id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS
                    JedenAktywnyNajemNaSprzet
                ON Wypozyczenie(IdSprzetu)
                WHERE DataZwrotu IS NULL;
                """;

            polecenie.ExecuteNonQuery();


            polecenie.CommandText = """
                INSERT OR IGNORE INTO Sprzet (Id, Nazwa, Typ)
                VALUES
                    ('L01', 'Laptop Dell', 'Laptop'),
                    ('L02', 'Laptop Lenovo', 'Laptop'),
                    ('L03', 'Laptop HP', 'Laptop'),
                    ('P01', 'Projektor Epson', 'Projektor'),
                    ('P02', 'Projektor BenQ', 'Projektor');
                """;

            polecenie.ExecuteNonQuery();
        }
    }
}