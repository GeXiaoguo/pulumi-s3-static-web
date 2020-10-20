# Pulumi C# Walkthrough - S3 Static Web

### Pulumi install
    runas /user:administrator "choco install pulumi"
    runas /user:administrator "choco upgrade pulumi"
    pulumi version

### Use the local file system as state storage
    mkdir local-state
    pulumi login file://./state
    // pulumi logout

### Create a csharp pulumi project
    mkdir pulumi
    cd pulumi
    pulumi new aws-cscharp

### List the pulumi configuration
    pulumi config

### Create a s3 bucket and dump a s3 object to it
    public MyStack()
    {
        // Create an AWS resource (S3 Bucket)
        var bucket = new Bucket("s3-static-web-html-bucket");
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
    }

    [Output]
    public Output<string> BucketName { get; set; }

### Deploy the stack
    pulumi up

### Show stack output
    pulumi stack output BucketName

### Inspect s3 content
    aws s3 ls $(pulumi stack output BucketName)

### Change the bucket definition to be a public website

        var bucket = new Bucket("s3-static-web-html-bucket", new BucketArgs
        {
            Website = new BucketWebsiteArgs
            {
                IndexDocument = "index.html"
            }
        });

### export the url
    this.WebSiteEndPoint = bucket.WebsiteEndpoint;

    [Output]
    public Output<string> WebSiteEndPoint { get; set; }

### Allow any body to read any object in the bucket
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

### Destroy the stack
    pulumi destroy

### Inspect s3 buckets
    aws s3 ls

### Next [Pulumi C# Walkthrough - Lambda](./readme01-lambda.md)
