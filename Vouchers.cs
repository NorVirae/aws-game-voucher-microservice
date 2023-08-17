using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsGameVoucherSystem
{
    [DynamoDBTable("Vouchers")]
    public class Vouchers
    {
        public string Email;
        public int GoldQuantity;
    }

    public class Voucher
    {
        public string Email;
        public string Id;
        public string VoucherCode;
        public int VoucherGoldQuantity;
        public DateTime CreationDate;
        public DateTime Expiry;
    }
    public class ConsumeVoucherRequest
    {
        public string Email;
        public string VoucherCode;
    }

}
