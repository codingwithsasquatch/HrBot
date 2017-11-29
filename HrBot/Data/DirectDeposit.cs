using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace HrBot.Data
{
    public class DirectDeposit : Resource
    {
        public string Employee { get; set; }
        public string PayType { get; set; }
        public string PaymentType { get; set; }
        public string Account { get; set; }
        public string AccountNumber { get; set; }
        public string Distribution { get; set; }
    }
}