
# Pulumi C# Walkthrough - S3 Static Web
# pulumi setup
    runas /user:administrator "choco install pulumi"
    runas /user:administrator "choco upgrade pulumi"
    pulumi version

# pulumi project setup
## using local file system as state storage
    mkdir s3-static-web
    mkdir local-state
    cd s3-static-web
    pulumi login file://./state
    // pulumi logout

## create a csharp pulumi project
    pulumi new aws-cscharp

## list the pulumi configuration
    pulumi config

## create a s3 bucket and dump a s3 object to it
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

# deploy the stack
    pulumi up

# show stack output
    pulumi stack output BucketName

# inspect s3 content
    aws s3 ls $(pulumi stack output BucketName)

# make the s3 bucket public
## change the bucket definition

        var bucket = new Bucket("s3-static-web-html-bucket", new BucketArgs
        {
            Website = new BucketWebsiteArgs
            {
                IndexDocument = "index.html"
            }
        });

## export the url
    this.WebSiteEndPoint = bucket.WebsiteEndpoint;

    [Output]
    public Output<string> WebSiteEndPoint { get; set; }

## allow any body to read any object in the bucket
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

# destroy the stack
    pulumi destroy

# inspect s3 buckets
    aws s3 ls