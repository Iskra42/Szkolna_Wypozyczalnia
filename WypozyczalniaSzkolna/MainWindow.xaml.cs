using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private bool _odswiezanieTypow;

        public MainWindow()
        {
            InitializeComponent();

            BazaDanych.Inicjalizuj();

            _sprzet.AddRange(BazaDanych.PobierzSprzet());
            _historia.AddRange(BazaDanych.PobierzWypozyczenia());

            txtSzukaj.TextChanged += (s, e) => OdswiezListe();
            cmbTyp.SelectionChanged += (s, e) =>
            {
                if (!_odswiezanieTypow)
                    OdswiezListe();
            };

            OdswiezTypy();
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
                    || s.Typ.Equals(
                        wybranyTyp,
                        StringComparison.OrdinalIgnoreCase))
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

        private void OdswiezTypy()
        {
            string zaznaczony =
                (cmbTyp.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? "Cały sprzęt";

            _odswiezanieTypow = true;

            cmbTyp.Items.Clear();
            cmbTyp.Items.Add(new ComboBoxItem { Content = "Cały sprzęt" });

            foreach (string typ in PobierzTypy())
                cmbTyp.Items.Add(new ComboBoxItem { Content = typ });

            cmbTyp.SelectedItem = cmbTyp.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(
                    i.Content?.ToString(),
                    zaznaczony,
                    StringComparison.OrdinalIgnoreCase))
                ?? cmbTyp.Items[0];

            _odswiezanieTypow = false;
        }

        private List<string> PobierzTypy()
        {
            return _sprzet
                .Select(s => s.Typ)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        // Jeśli typ już istnieje (bez względu na wielkość liter),
        // używamy jego istniejącego zapisu.
        private string DopasujTyp(string typ)
        {
            return PobierzTypy().FirstOrDefault(t =>
                t.Equals(typ, StringComparison.OrdinalIgnoreCase))
                ?? typ;
        }

        private void btnDodaj_Click(
            object sender, RoutedEventArgs e)
        {
            string id = "";
            string nazwa = "";
            string typ = "";

            while (true)
            {
                var okno = new SprzetOkno(
                    "Dodawanie sprzętu", PobierzTypy(),
                    id, nazwa, typ, idEdytowalne: true)
                {
                    Owner = this
                };

                if (okno.ShowDialog() != true)
                    return;

                id = okno.IdSprzetu;
                nazwa = okno.Nazwa;
                typ = DopasujTyp(okno.Typ);

                if (_sprzet.Any(s => s.Id.Equals(
                        id, StringComparison.OrdinalIgnoreCase)))
                {
                    PokazBlad($"Sprzęt o ID \"{id}\" już istnieje.");
                    continue;
                }

                break;
            }

            try
            {
                BazaDanych.DodajSprzet(id, nazwa, typ);
            }
            catch (SqliteException)
            {
                PokazBlad("Nie udało się zapisać sprzętu w bazie danych.");
                return;
            }

            _sprzet.Add(new Sprzet(id, nazwa, typ));
            _sprzet.Sort((a, b) => string.Compare(
                a.Id, b.Id, StringComparison.OrdinalIgnoreCase));

            OdswiezTypy();
            OdswiezListe();

            MessageBox.Show(
                $"Dodano sprzęt: {nazwa} ({id}), typ: {typ}.",
                "Sprzęt dodany",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void btnEdytuj_Click(
            object sender, RoutedEventArgs e)
        {
            string id = dgSprzet.SelectedItem is WierszSprzetu wiersz
                ? wiersz.Id
                : txtIdSprzetu.Text.Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                PokazBlad("Zaznacz sprzęt na liście lub wpisz jego ID.");
                return;
            }

            int indeks = _sprzet.FindIndex(s =>
                s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (indeks < 0)
            {
                PokazBlad("Nie znaleziono sprzętu o podanym ID.");
                return;
            }

            Sprzet obecny = _sprzet[indeks];

            var okno = new SprzetOkno(
                "Edycja sprzętu", PobierzTypy(),
                obecny.Id, obecny.Nazwa, obecny.Typ,
                idEdytowalne: false)
            {
                Owner = this
            };

            if (okno.ShowDialog() != true)
                return;

            string nazwa = okno.Nazwa;
            string typ = DopasujTyp(okno.Typ);

            try
            {
                BazaDanych.AktualizujSprzet(obecny.Id, nazwa, typ);
            }
            catch (SqliteException)
            {
                PokazBlad("Nie udało się zapisać zmian w bazie danych.");
                return;
            }

            _sprzet[indeks] = new Sprzet(obecny.Id, nazwa, typ);

            OdswiezTypy();
            OdswiezListe();

            MessageBox.Show(
                $"Zapisano zmiany sprzętu {obecny.Id}.",
                "Sprzęt zaktualizowany",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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

            DateTime teraz = DateTime.Now;
            int numer;

            try
            {
                numer = BazaDanych.DodajWypozyczenie(sprzet.Id, osoba, teraz);
            }
            catch (SqliteException)
            {
                PokazBlad("Nie udało się zapisać wypożyczenia. Sprzęt może być już wypożyczony.");
                return;
            }

            string idWypozyczenia = $"W{numer:000}";

            var wypozyczenie = new Wypozyczenie
            {
                Id = idWypozyczenia,
                IdSprzetu = sprzet.Id,
                Osoba = osoba,
                DataWypozyczenia = teraz,
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

            Sprzet sprzet = _sprzet.FirstOrDefault(s =>
                s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

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

            DateTime teraz = DateTime.Now;

            try
            {
                BazaDanych.ZakonczWypozyczenie(wypozyczenie.Id, teraz);
            }
            catch (SqliteException)
            {
                PokazBlad("Nie udało się zapisać zwrotu w bazie danych.");
                return;
            }

            wypozyczenie.CzyAktywne = false;
            wypozyczenie.DataZwrotu = teraz;

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

    /// <summary>
    /// Okno dialogowe do dodawania i edycji sprzętu (budowane w kodzie).
    /// </summary>
    public class SprzetOkno : Window
    {
        private readonly TextBox _txtId = new();
        private readonly TextBox _txtNazwa = new();
        private readonly ComboBox _cmbTyp = new() { IsEditable = true };

        public string IdSprzetu => _txtId.Text.Trim();
        public string Nazwa => _txtNazwa.Text.Trim();
        public string Typ => _cmbTyp.Text.Trim();

        public SprzetOkno(
            string tytul,
            IEnumerable<string> istniejaceTypy,
            string id = "",
            string nazwa = "",
            string typ = "",
            bool idEdytowalne = true)
        {
            Title = tytul;
            Width = 380;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xF4, 0xF7, 0xF2));

            _txtId.Text = id;
            _txtId.IsEnabled = idEdytowalne;
            _txtNazwa.Text = nazwa;

            foreach (string t in istniejaceTypy)
                _cmbTyp.Items.Add(t);
            _cmbTyp.Text = typ;

            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(Etykieta("ID sprzętu:"));
            panel.Children.Add(Pole(_txtId));
            panel.Children.Add(Etykieta("Nazwa:"));
            panel.Children.Add(Pole(_txtNazwa));
            panel.Children.Add(Etykieta("Typ (wybierz lub wpisz nowy):"));
            panel.Children.Add(Pole(_cmbTyp));

            var btnOk = new Button
            {
                Content = "Zapisz",
                Width = 100,
                Height = 34,
                IsDefault = true,
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnOk.Click += (s, e) => Zatwierdz();

            var btnAnuluj = new Button
            {
                Content = "Anuluj",
                Width = 100,
                Height = 34,
                IsCancel = true
            };

            panel.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 18, 0, 0),
                Children = { btnOk, btnAnuluj }
            });

            Content = panel;

            Loaded += (s, e) =>
            {
                if (idEdytowalne) _txtId.Focus();
                else _txtNazwa.Focus();
            };
        }

        private void Zatwierdz()
        {
            if (string.IsNullOrWhiteSpace(IdSprzetu)
                || string.IsNullOrWhiteSpace(Nazwa)
                || string.IsNullOrWhiteSpace(Typ))
            {
                MessageBox.Show(
                    this,
                    "Uzupełnij ID, nazwę i typ sprzętu.",
                    "Informacja",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private static TextBlock Etykieta(string tekst) => new()
        {
            Text = tekst,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 4)
        };

        private static Control Pole(Control c)
        {
            c.Height = 32;
            c.VerticalContentAlignment = VerticalAlignment.Center;
            if (c is TextBox) c.Padding = new Thickness(6, 0, 6, 0);
            return c;
        }
    }
}