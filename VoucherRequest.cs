using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsGameVoucherSystem
{
    [DynamoDBTable("Vouchers")]
    public class VoucherRequest
    {
        public string Email;
        public string Id;
        public string VoucherCode;
        public DateTime CreationDate;
        public DateTime Expiry;
    }
}
