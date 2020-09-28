
# Pulumi C# Walkthrough - Lambda
## create a classlib project for the lambda function
in a new folder 
    mkdir csharp-lambda-lib
    cd csharp-lambda-lib
    dotnet new classlib

### define a csharp function for the lambda
    // Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
    // important if want to have a FunctionHandler that does not take Stream as input
    [assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

    public class Function
    {
        public string FunctionHandler(string input, ILambdaContext context) => $"Hello {input}";
    }

### add nuget packages and publish the project
    dotnet add package Amazon.Lambda.Core
    dotnet add package Amazon.Lambda.Serialization.Json
    dotnet publish //p:GenerateRuntimeConfigurationFiles=true csharp-lambda-lib.csproj
    //GenerateRuntimeConfigurationFiles is important because by default, for a classlib project, dotnet publish will not create a runtimeconfig.json file, which AWS lambda demends

### inspect the published artifacts in 
    csharp-lambda-lib/bin/debug/dotnetcore3.1/publish/

## define pulumi stack for the lambda
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
            Code = new FileArchive("../csharp-lambda-lib/bin/debug/netstandard2.0/publish"),
            Handler = "csharp_lambda_lib::Function:FunctionHandler",
            Role = lambdaRole.Arn,
        });

## deploy the stack
    pulumi up

## inspect the deployment result
    pulumi stack output

## test the lambda 
    aws lambda invoke --function-name $(pulumi stack output Lambda) --cli-binary-format raw-in-base64-out --payload '"xiaoguo"' output.json
    // --payload value has to be a json string, "xiaoguo" is a valid json string literal, single quote it as the paramter to --payload
    // --cli-binary-format raw-in-base64-out is important becuase aws cli by default take base64 encoded binary data as input

