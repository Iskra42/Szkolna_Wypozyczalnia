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

        public MainWindow()
        {
            InitializeComponent();
        }
    }
}