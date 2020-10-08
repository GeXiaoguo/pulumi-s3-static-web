# Pulumi C# API Gateway + Lambda Walkthrough - API Gateway

# This walkthrough builds on the [C# Lambda Walkthrough](./readme-lambda.md)

## Create a a new lambda to be triggered by APIGateway

### Add the nuget package to csharp-lambda-lib project
    dotnet add package Amazon.Lambda.APIGatewayEvents

### Add a new C# function for the new lambda
    // lambda for api gateway has to have this signature (document)[https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.APIGatewayEvents/README.md]
        public APIGatewayProxyResponse  APIGatewayHandler(APIGatewayProxyRequest input) => new APIGatewayProxyResponse{
            Body =  $"Hello {input}",
            StatusCode = 200,
        };

## Publish the C# library
    dotnet publish //p:GenerateRuntimeConfigurationFiles=true csharp-lambda-lib.csproj

## Create the new lambda resources
    var lambda = new Function("apiGatewayLambda", new FunctionArgs
    {
        Runtime = "dotnetcore3.1",
        Code = new FileArchive("../csharp-lambda-lib/bin/debug/netcoreapp3.1/publish"),
        Handler = "csharp-lambda-lib::csharp_lambda_lib.Function::APIGatewayHandler",
        Role = lambdaRole.Arn,
    });
        
## Create pulumi resources for a restAPI for the new lambda
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
            Function = lambda.Arn,
            Principal = "apigateway.amazonaws.com",
            SourceArn = deployment.ExecutionArn
        });

        return deployment.InvokeUrl.Apply(x=>$"{x}test-stage");

## Deploy the stack
    pulumi up

## Inspect the deployment result
    pulumi stack output
    // sample api path: http://2gcx23nt87.execute-api.ap-southeast-2.amazonaws.com/test-stage/{proxy+}

## Test the api
    curl --header "Content-Type: application/json" --request POST --data '{"a":"b",}' $( pulumi stack output APIEndpoint)/{proxy+}
    //expected otuput: Hello {"a":"b",}

## Turn on CloudWatch log for trouble shooting API Gateway
https://seed.run/blog/how-to-enable-execution-logs-for-api-gateway.html
   

# Next see the [Pulumi C# Consuming Output in Frontend App](./readme-consuming-pulumi-output.md)