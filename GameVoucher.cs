using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
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

          
            if (request.Body != null)
            {
                var bodyContent = JsonConvert.DeserializeObject<VoucherRequest>(request.Body);
            }



            

            return response;
        }
        public async Task<APIGatewayProxyResponse> ConsumeVoucher(APIGatewayProxyRequest request)
        {

        }

        private void GenerateAndStoreVouchers(int count)
        {

        }

    }
}