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

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace AwsGameVoucherSystem
{
    public class GameVoucher
    {

        private void Init()
        {
            PlayFabSettings.staticSettings.DeveloperSecretKey = "Z85MNMYUMKHD8HA66JTIFT1UKSOWGBUWETZABX6CJ7O7UWQDCM";
            PlayFabSettings.staticSettings.TitleId = "10f24";

        }
        public async Task<APIGatewayProxyResponse> PurchaseVoucher(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Init();
            var response = new APIGatewayProxyResponse()
            {
                Headers = new Dictionary<string, string>(),
                StatusCode = 200,
                Body = "Its Empty"
            };

            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Headers", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "OPTIONS, post");
            response.Headers.Add("Content-Type", "application/json");

            //var bodyContent = request.IsBase64Encoded ? Convert.FromBase64String(request.Body) : Encoding.UTF8.GetBytes(request.Body);
            //using var memStream = new MemoryStream(bodyContent);
            //var dbRegion = Environment.GetEnvironmentVariable("DB_REGION");
            //var dbClient = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(dbRegion));
            try
            {
                Console.WriteLine(request.Body + " CHECk");
                if (request.Body != null && request.Body != "")
                {
                    var bodyContent = JsonConvert.DeserializeObject<Vouchers>(request.Body);
                   var resultData = await GenerateAndStoreVoucher(bodyContent.Email, bodyContent.GoldQuantity);
                    response.Body = JsonConvert.SerializeObject(new { Email = bodyContent.Email, VoucherCode = resultData.voucherCode });

                }
                else
                {
                    response.Body = JsonConvert.SerializeObject(new { Success = false, ErrorBody = request.Body, Error = "Failed: Unable to generate voucher code, Email or VoucherGoldQuantity empty" });
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
                return response;
            }
            catch (Exception ex)
            {
                response.Body = JsonConvert.SerializeObject(new {Success = false, Error = ex.Message });
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }

            
        }
        public async Task<APIGatewayProxyResponse> RedeemVoucher(APIGatewayProxyRequest request)
        {
            Init();

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
            //var dbRegion = Environment.GetEnvironmentVariable("DB_REGION");
            //var dbClient = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(dbRegion));
            
            try
            {
                if (request.Body != null)
                {
                    var bodyContent = JsonConvert.DeserializeObject<ConsumeVoucherRequest>(request.Body);

                    var voucherData = await GetPlayFabTitleData();
                    if (voucherData != null && voucherData.Data != null)
                    {
                        Dictionary<string, Voucher> newVoucherData;

                        //run migrations
                        if (voucherData.Data.ContainsKey("vouchers")){
                            newVoucherData = JsonConvert.DeserializeObject<Dictionary<string, Voucher>>(voucherData.Data["vouchers"]);

                            

                            if (newVoucherData.ContainsKey(bodyContent.VoucherCode))
                            {
                                var targetVoucher = newVoucherData[bodyContent.VoucherCode];
                                targetVoucher.isConsumed = true;
                                newVoucherData[bodyContent.VoucherCode] = targetVoucher;
                                var success = await SetPlayFabTitleDataMulti(newVoucherData);
                                if (success)
                                {
                                    response.Body = JsonConvert.SerializeObject(new { Success = true,
                                        Message = "Voucher has been consumed and used",
                                        VoucherCode= bodyContent.VoucherCode,
                                        GoldQuantity = targetVoucher .VoucherGoldQuantity
                                    });
                                }
                                else
                                {
                                    response.Body = JsonConvert.SerializeObject(new { Success = false, Error = "Unable to consume voucher" });
                                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                                }
                            }
                            else
                            {
                                response.Body = JsonConvert.SerializeObject(new { Success = false, Error = "Failed: Voucher has been used or doesn't exist!" });
                                response.StatusCode = (int)HttpStatusCode.BadRequest;
                            }
                        }
                        else
                        {
                            response.Body = JsonConvert.SerializeObject(new { Success = false, Error = "Failed: Voucher Does not exist!" });
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Body = JsonConvert.SerializeObject(new { Error = ex.Message });
                response.StatusCode = (int)HttpStatusCode.OK;
            }

            return response;
        }

        private async Task<VoucherGenerationResponse> GenerateAndStoreVoucher(string email, int goldQuantity) 
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

            var result = await SetPlayFabTitleDataSingle(voucher, newVoucherIdCode);

            if (result)
            {
                return new VoucherGenerationResponse { success = true, voucherCode = newVoucherIdCode };
            }
            else
            {
                return new VoucherGenerationResponse { success = false, voucherCode = "" };

            }
        }


        private async Task<GetTitleDataResult> GetPlayFabTitleData()
        {
            var keys = new List<string> { "vouchers"};
            var result = await PlayFabAdminAPI.GetTitleDataAsync(new GetTitleDataRequest() { Keys = keys });

            return result.Result;
        }


        private async Task<bool> SetPlayFabTitleDataSingle(Voucher voucher, string voucherKey)
        {
            var voucherData = await GetPlayFabTitleData();

            var voucherDataProcessed = voucherData.Data;
            if (voucherData != null && voucherDataProcessed != null)
            {
                Dictionary<string, Voucher> VoucherDataDeserialised;

                //implement Playfab data migration if key doesn't exist crete one
                if (voucherDataProcessed.ContainsKey("vouchers"))
                {
                    VoucherDataDeserialised = JsonConvert.DeserializeObject<Dictionary<string, Voucher>>(voucherDataProcessed["vouchers"]);
                }
                else
                {
                    VoucherDataDeserialised = new Dictionary<string, Voucher>();
                }

                var newModifiedVoucherData = VoucherDataDeserialised;
                newModifiedVoucherData?.Add(voucherKey, voucher);
                var result = await PlayFabAdminAPI.SetTitleDataAsync(new SetTitleDataRequest() { Key = "vouchers", Value = JsonConvert.SerializeObject(newModifiedVoucherData) });
                if (result != null && result.Result != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
            
        }

        private async Task<bool> SetPlayFabTitleDataMulti(Dictionary<string, Voucher> vouchers)
        {
            var result = await PlayFabAdminAPI.SetTitleDataAsync(new SetTitleDataRequest() { Key = "vouchers", Value = JsonConvert.SerializeObject(vouchers) });
            if (result != null && result.Result != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}