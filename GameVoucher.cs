using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Transform;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace AwsGameVoucherSystem
{
    public class GameVoucher
    {
        public async Task<APIGatewayProxyResponse> PurchaseVoucher(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var response = new APIGatewayProxyResponse()
            {
                Headers = new Dictionary<string, string>(),
                StatusCode = 200
            };

            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Headers", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "OPTIONS, post");
            response.Headers.Add("Content-Type", "application/json");

            //var bodyContent = request.IsBase64Encoded ? Convert.FromBase64String(request.Body) : Encoding.UTF8.GetBytes(request.Body);
            //using var memStream = new MemoryStream(bodyContent);
            var dbRegion = Environment.GetEnvironmentVariable("DB_REGION");
            var dbClient = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(dbRegion));
            try
            {
                if (request.Body != null)
                {
                    var bodyContent = JsonConvert.DeserializeObject<Vouchers>(request.Body);
                    var dbContext = new DynamoDBContext(dbClient);

                    string IdAndVoucherCode = Guid.NewGuid().ToString();
                    var voucher = new Voucher()
                    {
                        Email = bodyContent.Email,
                        Id = IdAndVoucherCode,
                        VoucherGoldQuantity = bodyContent.GoldQuantity,
                        VoucherCode = IdAndVoucherCode,

                        CreationDate = DateTime.Now,
                        Expiry = DateTime.Now.AddDays(20)
                    };
                    await dbContext.SaveAsync<Voucher>(voucher);


                    response.Body = JsonConvert.SerializeObject(new { Email = bodyContent.Email, VoucherCode = IdAndVoucherCode });

                }
            }
            catch (Exception ex)
            {
                response.Body = JsonConvert.SerializeObject(new { Error = ex.Message });
            }

            return response;
        }
        public async Task<APIGatewayProxyResponse> ConsumeVoucher(APIGatewayProxyRequest request)
        {
            var response = new APIGatewayProxyResponse()
            {
                Headers = new Dictionary<string, string>(),
                StatusCode = 200
            };

            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Headers", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "OPTIONS, post");
            response.Headers.Add("Content-Type", "application/json");

            //var bodyContent = request.IsBase64Encoded ? Convert.FromBase64String(request.Body) : Encoding.UTF8.GetBytes(request.Body);
            //using var memStream = new MemoryStream(bodyContent);
            var dbRegion = Environment.GetEnvironmentVariable("DB_REGION");
            var dbClient = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(dbRegion));
            try
            {
                if (request.Body != null)
                {
                    var bodyContent = JsonConvert.DeserializeObject<ConsumeVoucherRequest>(request.Body);

                    var key = new Dictionary<string, AttributeValue> {
                        ["VoucherCode"] = new AttributeValue { S = bodyContent.VoucherCode }
                    };
                    var reqs = new GetItemRequest
                    {
                        Key = key,
                        TableName = tableName,
                    };
                    await dbClient.GetItemAsync<Voucher>("Voucher", key, new CancellationToken { });

                    response.Body = JsonConvert.SerializeObject(new { Email = bodyContent.Email, VoucherCode = IdAndVoucherCode });

                }
            }
            catch (Exception ex)
            {
                response.Body = JsonConvert.SerializeObject(new { Error = ex.Message });
            }

            return response;
        }

        private void GenerateAndStoreVouchers(int count)
        {

        }

    }
}