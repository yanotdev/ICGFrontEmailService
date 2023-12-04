using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICGEmailService.Models
{
    public class NewSales
    {
        public int InvoiceNumber { get; set; }
        public int CustomerId { get; set; }
        public string Serie { get; set; }
        public decimal Cost { get; set; }
    }
}
