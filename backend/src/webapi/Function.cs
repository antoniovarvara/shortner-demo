using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Util;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.DataModel;
using System.Net;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace LambdaFunction
{
    public class Function
    {
        public static AmazonLambdaClient lambdaClient;
        public static ShortUrlTable table;
        private static Random random = new Random();

        static Function()
        {
            initialize();
        }

        static async void initialize()
        {
            AWSSDKHandler.RegisterXRayForAllServices();
            lambdaClient = new AmazonLambdaClient();
            AmazonDynamoDBClient dynamodbClient = new AmazonDynamoDBClient();
            DynamoDBContext ctx = new DynamoDBContext(dynamodbClient);
            table = new ShortUrlTable(ctx);
            await callLambda();
        }

        public static string createId(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());

        }

        public static string createCompleteUrl(string resourceId)
        {
            string apiId = System.Environment.GetEnvironmentVariable("REDIRECT_API");
            string environment = System.Environment.GetEnvironmentVariable("ENV");
            string region = System.Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
            return "https://" + apiId + ".execute-api." + region + ".amazonaws.com/" + environment + "/" + resourceId;
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {

            //string tableName = System.Environment.GetEnvironmentVariable("DYNAMO_TABLE");
            LambdaLogger.Log("EVENT: " + JsonConvert.SerializeObject(apigProxyEvent));
            try
            {
                string resourceId = null;
                int idLength = 1;
                if (apigProxyEvent.Path == "/shortUrl")
                {
                    resourceId = createId(idLength);
                    LambdaLogger.Log("Test environment for set");

                    while (true)
                    {
                        resourceId = createId(idLength);
                        LambdaLogger.Log("Generated id " + resourceId);
                        ShortUrlItem item = await table.get(resourceId);
                        if (item == null)
                        {
                            ApiUrlRequestBody requestBody = JsonConvert.DeserializeObject<ApiUrlRequestBody>(apigProxyEvent.Body);
                            await table.save(new ShortUrl(resourceId, requestBody.originalUrl).dynamoItem);
                            break;
                        }
                        LambdaLogger.Log("ITEM: " + item.ToString() + " ID: " + item.resourceId);
                        idLength++;
                    }
                    return CreateBasicResponse(new ApiBody
                    {
                        resourceId = resourceId,
                        completeUrl = createCompleteUrl(resourceId)
                    });
                }
                else
                {
                    LambdaLogger.Log("Test environment for get");
                    ShortUrlItem item = await table.get(apigProxyEvent.PathParameters["resourceId"]);

                    LambdaLogger.Log("ITEM: " + item.ToString() + " ID: " + item.resourceId);

                    resourceId = item.resourceId;
                    if(item.ttl < new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds())
                    {
                        throw new Exception("Expired url");
                    }

                    return CreateRedirectResponse(item.originalUrl); 
                }

                
            } catch(Exception e)
            {
                return CreateErrorResponse();
            }
            
        }

        APIGatewayProxyResponse CreateErrorResponse()
        {
            string errorEndpoint = System.Environment.GetEnvironmentVariable("ERROR_URL");
            int statusCode = (int)HttpStatusCode.Redirect;

            string body = string.Empty;

            var response = new APIGatewayProxyResponse
            {
                StatusCode = statusCode,
                Body = body,
                Headers = new Dictionary<string, string>
                {
                    { "Location",  errorEndpoint + "/error"},
                    { "Access-Control-Allow-Origin", "*" }
                }
            };

            return response;
        }

        APIGatewayProxyResponse CreateBasicResponse(ApiBody? responseBody)
        {
            int statusCode = (responseBody != null) ?
                (int)HttpStatusCode.OK :
                (int)HttpStatusCode.InternalServerError;

            string body = (responseBody != null) ?
                JsonConvert.SerializeObject(responseBody) : string.Empty;

            var response = new APIGatewayProxyResponse
            {
                StatusCode = statusCode,
                Body = body,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                }
            };

            return response;
        }

        APIGatewayProxyResponse CreateRedirectResponse(string redirectUrl)
        {
            int statusCode = (int)HttpStatusCode.Redirect;

            string body = string.Empty;
            var completeRedirectUrl = redirectUrl;
            if (!redirectUrl.StartsWith("http://") && !redirectUrl.StartsWith("https://"))
            {
                completeRedirectUrl = "https://" + redirectUrl;
            }

            var response = new APIGatewayProxyResponse
            {
                StatusCode = statusCode,
                Body = body,
                Headers = new Dictionary<string, string>
                {
                    { "Location",  completeRedirectUrl},
                    { "Access-Control-Allow-Origin", "*" }
                }
            };

            return response;
        }

        public static async Task<GetAccountSettingsResponse> callLambda()
        {
            var request = new GetAccountSettingsRequest();
            var response = await Function.lambdaClient.GetAccountSettingsAsync(request);
            return response;
        }
    }

    public class ApiBody
    {
        public string resourceId;
        public string completeUrl;
    }


    public class ApiUrlRequestBody
    {
        public string originalUrl { get; set; }
    }

    public class ShortUrlTable
    {
        private DynamoDBContext context;
        public ShortUrlTable(DynamoDBContext context)
        {
            this.context = context;
        }

        public async Task<bool> save(ShortUrlItem urlItem)
        {
            await context.SaveAsync(urlItem);
            return true;
        }

        public Task<ShortUrlItem> get(string resourceId)
        {
            return context.LoadAsync<ShortUrlItem>(resourceId);
        }
    }
    public class ShortUrl
    {
        public ShortUrlItem dynamoItem;
        public ShortUrl(string resourceId, string originalUrl)
        {
            this.dynamoItem = new ShortUrlItem
            {
                resourceId = resourceId,
                originalUrl = originalUrl,
                ttl = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + 120
                };
        }
    }

    [DynamoDBTable("shortnerdemo_dev_ddb_registered_urls")]
    public class ShortUrlItem
    {
        /*public ShortUrlItem(string resourceId, string originalUrl)
        {
            this.resourceId = resourceId;
            this.originalUrl = originalUrl;
            this.ttl = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()+ 120 * 1000;
        }*/

        [DynamoDBHashKey] //Partition key
        public string resourceId
        {
            get; set;
        }
        [DynamoDBProperty]
        public string originalUrl
        {
            get; set;
        }
        [DynamoDBProperty]
        public long ttl
        {
            get; set;
        }
    }
}