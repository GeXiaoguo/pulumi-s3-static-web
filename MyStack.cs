using System;
using Pulumi;
using Pulumi.Aws.S3;
using Pulumi.Aws.S3.Inputs;

class MyStack : Stack
{
    public MyStack()
    {
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
    }

    [Output]
    public Output<string> BucketName { get; set; }
    [Output]
    public Output<string> WebSiteEndPoint { get; set; }
}
