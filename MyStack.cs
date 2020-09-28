using System;
using Pulumi;
using Pulumi.Aws.Iam;
using Pulumi.Aws.Lambda;
using Pulumi.Aws.S3;
using Pulumi.Aws.S3.Inputs;

class MyStack : Stack
{
    public MyStack()
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
            
        // Create an AWS resource (S3 Bucket)
        var bucket = new Bucket("s3-static-web-html-bucket", new BucketArgs
        {
            Website = new BucketWebsiteArgs
            {
                IndexDocument = "index.html"
            }
        });
        
        Func<string, string> publicS3ReadPolicyFunc = bucketId=> $@"{{
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

        // Export the name of the bucket
        this.BucketName = bucket.Id;
        this.WebSiteEndPoint = bucket.WebsiteEndpoint;
        this.Lambda = lambda.Arn;
    }

    [Output]
    public Output<string> Lambda { get; set; }

    [Output]
    public Output<string> BucketName { get; set; }
    
    [Output]
    public Output<string> WebSiteEndPoint { get; set; }
}
