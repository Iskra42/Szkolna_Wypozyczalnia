using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WypozyczalniaSzkolna.Models
{
    public class Sprzet
    {
        public string Id { get; set; }
        public string Nazwa { get; set; }
        public string Typ { get; set; }

        public Sprzet(string id, string nazwa, string typ)
        {
            Id = id;
            Nazwa = nazwa;
            Typ = typ;
        }
    }
}
