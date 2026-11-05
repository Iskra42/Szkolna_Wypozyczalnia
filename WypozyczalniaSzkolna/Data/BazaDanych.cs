using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using WypozyczalniaSzkolna.Models;

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

        private static SqliteConnection Otworz()
        {
            var polaczenie = new SqliteConnection(Polaczenie);
            polaczenie.Open();


            using var cmd = polaczenie.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();

            return polaczenie;
        }

        public static List<Sprzet> PobierzSprzet()
        {
            var lista = new List<Sprzet>();

            using var polaczenie = Otworz();
            using var cmd = polaczenie.CreateCommand();
            cmd.CommandText = "SELECT Id, Nazwa, Typ FROM Sprzet ORDER BY Id;";

            using var r = cmd.ExecuteReader();
            while (r.Read())
                lista.Add(new Sprzet(r.GetString(0), r.GetString(1), r.GetString(2)));

            return lista;
        }

        public static void DodajSprzet(string id, string nazwa, string typ)
        {
            using var polaczenie = Otworz();
            using var cmd = polaczenie.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Sprzet (Id, Nazwa, Typ)
                VALUES ($id, $nazwa, $typ);
                """;
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$nazwa", nazwa);
            cmd.Parameters.AddWithValue("$typ", typ);
            cmd.ExecuteNonQuery();
        }

        public static void AktualizujSprzet(string id, string nazwa, string typ)
        {
            using var polaczenie = Otworz();
            using var cmd = polaczenie.CreateCommand();
            cmd.CommandText = """
                UPDATE Sprzet
                SET Nazwa = $nazwa, Typ = $typ
                WHERE Id = $id;
                """;
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$nazwa", nazwa);
            cmd.Parameters.AddWithValue("$typ", typ);
            cmd.ExecuteNonQuery();
        }

        public static List<Wypozyczenie> PobierzWypozyczenia()
        {
            var lista = new List<Wypozyczenie>();

            using var polaczenie = Otworz();
            using var cmd = polaczenie.CreateCommand();
            cmd.CommandText = """
                SELECT Id, IdSprzetu, Osoba, DataWypozyczenia, DataZwrotu
                FROM Wypozyczenie
                ORDER BY Id;
                """;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                DateTime? zwrot = r.IsDBNull(4)
                    ? null
                    : DateTime.Parse(r.GetString(4), null, DateTimeStyles.RoundtripKind);

                lista.Add(new Wypozyczenie
                {
                    Id = $"W{r.GetInt32(0):000}",
                    IdSprzetu = r.GetString(1),
                    Osoba = r.GetString(2),
                    DataWypozyczenia = DateTime.Parse(
                        r.GetString(3), null, DateTimeStyles.RoundtripKind),
                    DataZwrotu = zwrot,
                    CzyAktywne = zwrot == null
                });
            }

            return lista;
        }


        public static int DodajWypozyczenie(string idSprzetu, string osoba, DateTime data)
        {
            using var polaczenie = Otworz();
            using var cmd = polaczenie.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Wypozyczenie (IdSprzetu, Osoba, DataWypozyczenia)
                VALUES ($sprzet, $osoba, $data);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("$sprzet", idSprzetu);
            cmd.Parameters.AddWithValue("$osoba", osoba);
            cmd.Parameters.AddWithValue("$data", data.ToString("o"));

            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        public static void ZakonczWypozyczenie(string idWypozyczenia, DateTime data)
        {
            int numer = int.Parse(idWypozyczenia.Substring(1));

            using var polaczenie = Otworz();
            using var cmd = polaczenie.CreateCommand();
            cmd.CommandText = """
                UPDATE Wypozyczenie
                SET DataZwrotu = $data
                WHERE Id = $id AND DataZwrotu IS NULL;
                """;
            cmd.Parameters.AddWithValue("$data", data.ToString("o"));
            cmd.Parameters.AddWithValue("$id", numer);
            cmd.ExecuteNonQuery();
        }
    }
}