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

namespace WypozyczalniaSzkolna
{
    public partial class MainWindow : Window
    {
        private readonly List<Sprzet> _sprzet = new();
        private readonly List<Wypozyczenie> _historia = new();
        private int _kolejnyNumerWypozyczenia = 1;

        public class Sprzet
        {
            public string Id { get; set; }
            public string Nazwa {  get; set; }
            public string Typ {  get; set; }

            public Sprzet(string id, string nazwa, string typ)
            {
                Id = id;
                Nazwa = nazwa;
                Typ = typ;
            }
        }

        public class Wypozyczenie
        {
            public string Id { get; set; }
            public string IdSprzetu { get; set; }
            public string Osoba { get; set; }
            public DateTime DataWypozyczenia { get; set; }
            public DateTime? DataZwrotu { get; set; }
            public bool CzyAktywne { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();

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
    }
}