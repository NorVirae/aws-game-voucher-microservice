using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Transform;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.AdminModels;
using System.Diagnostics;
using System.Net;
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
                    GenerateAndStoreVoucher(bodyContent.Email, bodyContent.GoldQuantity, (bool success, string voucherCode) =>
                    {
                        response.Body = JsonConvert.SerializeObject(new { Email = bodyContent.Email, VoucherCode = voucherCode });

                    });


                }
                else
                {
                    response.Body = JsonConvert.SerializeObject(new { Success = false, Error = "Failed: Unable to generate voucher code" });
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            }
            catch (Exception ex)
            {
                response.Body = JsonConvert.SerializeObject(new {Success = false, Error = ex.Message });
                response.StatusCode = (int)HttpStatusCode.BadRequest;
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

                    GetPlayFabTitleData((Dictionary<string, Voucher> voucherData) => {
                        if (voucherData != null && voucherData[bodyContent.VoucherCode] != null)
                        {
                            var newVoucherData = voucherData;
                            var targetVoucher = newVoucherData[bodyContent.VoucherCode];
                            targetVoucher.isConsumed = true;
                            newVoucherData[bodyContent.VoucherCode] = targetVoucher;

                            SetPlayFabTitleDataMulti(newVoucherData, (bool success) =>
                            {
                                if (success)
                                {
                                    response.Body = JsonConvert.SerializeObject(new { Success = true, Message = "Voucher has been consumed and used" });
                                }
                                else
                                {
                                    response.Body = JsonConvert.SerializeObject(new { Success = false, Error = "Unable to consume voucher" });
                                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                                }
                            });
                        }

                    });
                }
            }
            catch (Exception ex)
            {
                response.Body = JsonConvert.SerializeObject(new { Error = ex.Message });
            }

            return response;
        }

        private void GenerateAndStoreVoucher(string email, int goldQuantity, Action<bool, string> callback) 
        {
            var newVoucherIdCode = Guid.NewGuid().ToString();
            var voucher = new Voucher()
            {
                Id = newVoucherIdCode,
                VoucherCode = newVoucherIdCode,
                VoucherGoldQuantity = goldQuantity,
                isConsumed = false,
                Email = email,
                CreationDate = DateTime.Now,
                Expiry = DateTime.Now.AddDays(10),

            };

            SetPlayFabTitleDataSingle(voucher, (bool success) =>
            {
                if (success)
                {
                    callback.Invoke(true, newVoucherIdCode);
                }
                else
                {
                    callback.Invoke(false, newVoucherIdCode);
                }
            });
        }


        private async void GetPlayFabTitleData(Action<Dictionary<string, Voucher>> callback)
        {
            var keys = new List<string> { "vouchers"};
            var result = await PlayFabAdminAPI.GetTitleDataAsync(new GetTitleDataRequest() { Keys = keys });
            if (result != null && result.Result != null)
            {
                var voucherObject = JsonConvert.DeserializeObject<Dictionary<string, Voucher>>(result.Result.Data["voucher"]);
                callback.Invoke(voucherObject);

            }
            else
            {
                callback.Invoke(null);
            }
        }
        private void SetPlayFabTitleDataSingle(Voucher voucher, Action<bool> callback)
        {
            GetPlayFabTitleData((Dictionary<string, Voucher> voucherData) =>
            {
                if (voucherData != null)
                {
                    string stringIdOrVoucherCode = Guid.NewGuid().ToString();
                    
                    var newModifiedVoucherData = voucherData;
                    newModifiedVoucherData.Add(stringIdOrVoucherCode, voucher);
                    var keys = new List<string> { "vouchers" };
                    var result = PlayFabAdminAPI.SetTitleDataAsync(new SetTitleDataRequest() { Key = "Voucher", Value = JsonConvert.SerializeObject(newModifiedVoucherData) });
                    if (result != null && result.Result != null)
                    {
                        callback.Invoke(true);

                    }
                    else
                    {
                        callback.Invoke(false);
                    }
                }
                else
                {
                    callback.Invoke(false);
                }
            });
        }

        private void SetPlayFabTitleDataMulti(Dictionary<string, Voucher> vouchers, Action<bool> callback)
        {
            var result = PlayFabAdminAPI.SetTitleDataAsync(new SetTitleDataRequest() { Key = "vouchers", Value = JsonConvert.SerializeObject(vouchers) });
            if (result != null && result.Result != null)
            {
                callback.Invoke(true);
            }
            else
            {
                callback.Invoke(false);
            }
        }
    }
}