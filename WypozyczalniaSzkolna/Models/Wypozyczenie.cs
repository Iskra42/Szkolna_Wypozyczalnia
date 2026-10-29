using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WypozyczalniaSzkolna.Models
{
    public class Wypozyczenie
    {
        public string Id { get; set; }
        public string IdSprzetu { get; set; }
        public string Osoba { get; set; }
        public DateTime DataWypozyczenia { get; set; }
        public DateTime? DataZwrotu { get; set; }
        public bool CzyAktywne { get; set; }
    }
}
