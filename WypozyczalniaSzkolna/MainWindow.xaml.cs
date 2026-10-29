using Microsoft.VisualBasic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WypozyczalniaSzkolna.Models;
using WypozyczalniaSzkolna.Data;

namespace WypozyczalniaSzkolna
{
    public partial class MainWindow : Window
    {
        private readonly List<Sprzet> _sprzet = new();
        private readonly List<Wypozyczenie> _historia = new();
        private int _kolejnyNumerWypozyczenia = 1;

        public MainWindow()
        {
            InitializeComponent();

            BazaDanych.Inicjalizuj();

            _sprzet.AddRange(new List<Sprzet>
            {
                new Sprzet("L01", "Laptop Dell", "Laptop"),
                new Sprzet("L02", "Laptop Lenovo", "Laptop"),
                new Sprzet("L03", "Laptop HP", "Laptop"),
                new Sprzet("P01", "Projektor Epson", "Projektor"),
                new Sprzet("P02", "Projektor BenQ", "Projektor")
            });

            txtSzukaj.TextChanged += (s, e) => OdswiezListe();
            cmbTyp.SelectionChanged += (s, e) => OdswiezListe();

            OdswiezListe();
        }

        public class WierszSprzetu
        {
            public string Id { get; set; }
            public string Nazwa { get; set; }
            public string Typ { get; set; }
            public string Dostepnosc { get; set; }
        }

        private bool CzyDostepny(string idSprzetu)
        {
            return !_historia.Any(w =>
                w.IdSprzetu.Equals(
                    idSprzetu,
                    StringComparison.OrdinalIgnoreCase) && w.CzyAktywne);
        }

        private void OdswiezListe()
        {
            string szukanaFraza = txtSzukaj.Text.Trim();

            string wybranyTyp =
                (cmbTyp.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? "Cały sprzęt";

            var wynik = _sprzet
                .Where(s =>
                    s.Id.Contains(
                        szukanaFraza,
                        StringComparison.OrdinalIgnoreCase)
                    || s.Nazwa.Contains(
                        szukanaFraza,
                        StringComparison.OrdinalIgnoreCase)
                    || s.Typ.Contains(
                        szukanaFraza,
                        StringComparison.OrdinalIgnoreCase))
                .Where(s =>
                    wybranyTyp == "Cały sprzęt"
                    || s.Typ == wybranyTyp)
                .Select(s => new WierszSprzetu
                {
                    Id = s.Id,
                    Nazwa = s.Nazwa,
                    Typ = s.Typ,
                    Dostepnosc = CzyDostepny(s.Id)
                        ? "Dostępny"
                        : "Wypożyczony"
                })
                .ToList();

            dgSprzet.ItemsSource = wynik;
        }

        private string PobierzIdSprzetu()
        {
            string id = txtIdSprzetu.Text.Trim();

            if (!string.IsNullOrWhiteSpace(id))
                return id;

            if (dgSprzet.SelectedItem is WierszSprzetu wybrany)
                return wybrany.Id;

            return string.Empty;
        }

        private void btnWypozycz_Click(
            object sender, RoutedEventArgs e)
        {
            string id = PobierzIdSprzetu();

            if (string.IsNullOrWhiteSpace(id))
            {
                PokazBlad("Wpisz ID sprzętu lub wybierz sprzęt z listy.");
                return;
            }

            Sprzet sprzet = _sprzet.FirstOrDefault(s =>
                s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (sprzet == null)
            {
                PokazBlad("Nie znaleziono sprzętu o podanym ID.");
                return;
            }

            if (!CzyDostepny(sprzet.Id))
            {
                PokazBlad("Ten sprzęt jest już wypożyczony.");
                return;
            }

            string osoba = Interaction.InputBox(
                "Podaj imię i nazwisko osoby wypożyczającej:",
                "Wypożyczenie sprzętu",
                "");

            osoba = osoba.Trim();

            if (string.IsNullOrWhiteSpace(osoba))
            {
                PokazBlad(
                    "Nie podano danych osoby. Wypożyczenie anulowano.");
                return;
            }

            string idWypozyczenia =
                $"W{_kolejnyNumerWypozyczenia:000}";

            _kolejnyNumerWypozyczenia++;

            var wypozyczenie = new Wypozyczenie
            {
                Id = idWypozyczenia,
                IdSprzetu = sprzet.Id,
                Osoba = osoba,
                DataWypozyczenia = DateTime.Now,
                DataZwrotu = null,
                CzyAktywne = true
            };

            _historia.Add(wypozyczenie);

            OdswiezListe();

            MessageBox.Show(
                $"Sprzęt: {sprzet.Nazwa}\n" +
                $"Osoba: {osoba}\n" +
                $"Numer wypożyczenia: {idWypozyczenia}\n" +
                $"Data: {wypozyczenie.DataWypozyczenia:dd.MM.yyyy HH:mm}",
                "Wypożyczenie zarejestrowane",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void btnZwroc_Click(
            object sender, RoutedEventArgs e)
        {
            string id = PobierzIdSprzetu();

            if (string.IsNullOrWhiteSpace(id))
            {
                PokazBlad(
                    "Wpisz ID sprzętu, który chcesz zwrócić.");
                return;
            }

            Sprzet sprzet = _sprzet.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (sprzet == null)
            {
                PokazBlad("Nie znaleziono sprzętu o podanym ID.");
                return;
            }

            Wypozyczenie wypozyczenie = _historia
                .Where(w =>
                    w.IdSprzetu.Equals(
                        sprzet.Id,
                        StringComparison.OrdinalIgnoreCase)
&& w.CzyAktywne)
                .OrderByDescending(w => w.DataWypozyczenia)
                .FirstOrDefault();

            if (wypozyczenie == null)
            {
                PokazBlad(
                    "Ten sprzęt nie ma aktywnego wypożyczenia.");
                return;
            }

            MessageBoxResult decyzja = MessageBox.Show(
                $"Czy przyjąć zwrot sprzętu {sprzet.Nazwa}?\n" +
                $"Osoba: {wypozyczenie.Osoba}\n" +
                $"Wypożyczenie: {wypozyczenie.Id}",
                "Potwierdzenie zwrotu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (decyzja != MessageBoxResult.Yes)
                return;

            wypozyczenie.CzyAktywne = false;
            wypozyczenie.DataZwrotu = DateTime.Now;

            OdswiezListe();

            MessageBox.Show(
                $"Zwrot sprzętu {sprzet.Nazwa} został zarejestrowany.\n" +
                $"Numer wypożyczenia: {wypozyczenie.Id}\n" +
                $"Data zwrotu: {wypozyczenie.DataZwrotu:dd.MM.yyyy HH:mm}",
                "Zwrot przyjęty",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        private void PokazBlad(string wiadomosc)
        {
            MessageBox.Show(
                wiadomosc,
                "Informacja",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}