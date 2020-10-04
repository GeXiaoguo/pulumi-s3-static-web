using System;
using Pulumi;
using Pulumi.Aws.Iam;
using Pulumi.Aws.Lambda;
using Pulumi.Aws.S3;
using Pulumi.Aws.S3.Inputs;
using Pulumi.Aws.ApiGateway;
using System.Text.Json;

class MyStack : Stack
{
    public MyStack()
    {
        Function lambda = CreateLambdaResources();
        Function apiGatewayLambda = CreateAPIGatewayLambdaResources();
        Bucket bucket = CreateS3BucketResources();
        var url = CreateRestAPIGatewayResources(apiGatewayLambda);

        this.BucketName = bucket.Id;
        this.WebSiteEndPoint = bucket.WebsiteEndpoint;
        this.Lambda = lambda.Arn;
        this.APIGatewayLambda = apiGatewayLambda.Arn;
        this.APIEndpoint = url;
    }

    private Output<string> CreateRestAPIGatewayResources(Function lambda)
    {
        var body = lambda.Arn.Apply(lambdaArn => $@"
                {{
                    ""swagger"" : ""2.0"",
                    ""info"" : {{""title"" : ""api"", ""version"" : ""1.0""}},
                    ""paths"" : {{
                        ""/{{proxy+}}"" : {{
                            ""x-amazon-apigateway-any-method"" : {{
                                ""x-amazon-apigateway-integration"" : {{
                                    ""uri"" : ""arn:aws:apigateway:ap-southeast-2:lambda:path/2015-03-31/functions/{lambdaArn}/invocations"",
                                    ""passthroughBehavior"" : ""when_no_match"",
                                    ""httpMethod"" : ""POST"",
                                    ""type"" : ""aws_proxy""
                                }}
                            }}
                        }}
                    }}
                }}");

        var restApiGateway = new RestApi("restAPI", new RestApiArgs
        {
            Body = body
        });

        var deployment = new Pulumi.Aws.ApiGateway.Deployment("dev-api", new DeploymentArgs
        {
            RestApi = restApiGateway.Id,//strange it demands Id instead of Arn
            StageName = ""
        });

        var stage = new Stage("dev-api-stage", new StageArgs
        {
            RestApi = restApiGateway.Id,
            Deployment = deployment.Id,
            StageName = "test-stage",
        });

        var invokePermission = new Permission("api-lambda-permission", new PermissionArgs
        {
            Action = "lambda:invokeFunction",
            Function = lambda.Name,
            Principal = "apigateway.amazonaws.com",
            SourceArn = deployment.ExecutionArn.Apply(x=>$"{x}*/*")
        });

        return deployment.InvokeUrl.Apply(x=>$"{x}test-stage");
    }
    private static Bucket CreateS3BucketResources()
    {
        // Create an AWS resource (S3 Bucket)
        var bucket = new Bucket("s3-static-web-html-bucket", new BucketArgs
        {
            Website = new BucketWebsiteArgs
            {
                IndexDocument = "index.html"
            }
        });

        Func<string, string> publicS3ReadPolicyFunc = bucketId => $@"{{
            ""Version"": ""2012-10-17"",
            ""Statement"": [{{
                ""Effect"": ""Allow"",
                ""Principal"": ""*"",
                ""Action"": [
                    ""s3:GetObject""
                ],
                ""Resource"": ""arn:aws:s3:::{bucketId}/*""
            }}]
        }}";

        var bucketPolicy = new BucketPolicy("bucketPolicy", new BucketPolicyArgs
        {
            Bucket = bucket.Id,
            Policy = bucket.Id.Apply(publicS3ReadPolicyFunc),
        });

        var s3Object = new BucketObject("index.html", new BucketObjectArgs
        {
            Bucket = bucket.BucketName,
            Content = @"<html>
                            <body>
                                <h1>Hello, Pulumi!</h1>
                            </body>
                        </html>",
        });
        return bucket;
    }
    private static Function CreateLambdaResources()
    {
        var lambdaRole = new Role("lambdaRole", new RoleArgs
        {
            AssumeRolePolicy =
                @"{
                ""Version"": ""2012-10-17"",
                ""Statement"": [
                    {
                        ""Action"": ""sts:AssumeRole"",
                        ""Principal"": {
                            ""Service"": ""lambda.amazonaws.com""
                        },
                        ""Effect"": ""Allow"",
                        ""Sid"": """"
                    }
                ]
            }"
        });

        var logPolicy = new RolePolicy("lambdaLogPolicy", new RolePolicyArgs
        {
            Role = lambdaRole.Id,
            Policy =
                @"{
                ""Version"": ""2012-10-17"",
                ""Statement"": [{
                    ""Effect"": ""Allow"",
                    ""Action"": [
                        ""logs:CreateLogGroup"",
                        ""logs:CreateLogStream"",
                        ""logs:PutLogEvents""
                    ],
                    ""Resource"": ""arn:aws:logs:*:*:*""
                }]
            }"
        });

        var lambda = new Function("basicLambda", new FunctionArgs
        {
            Runtime = "dotnetcore3.1",
            Code = new FileArchive("../csharp-lambda-lib/bin/debug/netcoreapp3.1/publish"),
            Handler = "csharp-lambda-lib::csharp_lambda_lib.Function::FunctionHandler",
            Role = lambdaRole.Arn,
        });
        return lambda;
    }
    private static Function CreateAPIGatewayLambdaResources()
    {
        var lambdaRole = new Role("apiGatewayLambdaRole", new RoleArgs
        {
            AssumeRolePolicy =
                @"{
                ""Version"": ""2012-10-17"",
                ""Statement"": [
                    {
                        ""Action"": ""sts:AssumeRole"",
                        ""Principal"": {
                            ""Service"": ""lambda.amazonaws.com""
                        },
                        ""Effect"": ""Allow"",
                        ""Sid"": """"
                    }
                ]
            }"
        });

        var logPolicy = new RolePolicy("apiGatewayLambdaLogPolicy", new RolePolicyArgs
        {
            Role = lambdaRole.Id,
            Policy =
                @"{
                ""Version"": ""2012-10-17"",
                ""Statement"": [{
                    ""Effect"": ""Allow"",
                    ""Action"": [
                        ""logs:CreateLogGroup"",
                        ""logs:CreateLogStream"",
                        ""logs:PutLogEvents""
                    ],
                    ""Resource"": ""arn:aws:logs:*:*:*""
                }]
            }"
        });

        var lambda = new Function("apiGatewayLambda", new FunctionArgs
        {
            Runtime = "dotnetcore3.1",
            Code = new FileArchive("../csharp-lambda-lib/bin/debug/netcoreapp3.1/publish"),
            Handler = "csharp-lambda-lib::csharp_lambda_lib.Function::APIGatewayHandler",
            Role = lambdaRole.Arn,
        });
        
        return lambda;
    }

    [Output]
    public Output<string> APIEndpoint { get; set; }

    [Output]
    public Output<string> Lambda { get; set; }

    [Output]
    public Output<string> APIGatewayLambda { get; set; }

    [Output]
    public Output<string> BucketName { get; set; }
    
    [Output]
    public Output<string> WebSiteEndPoint { get; set; }
}
