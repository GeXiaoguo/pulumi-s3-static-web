# Pulumi C# Consuming Output in Frontend App

### This walkthrough builds on the [Pulumi C# API Gateway + Lambda Walkthrough - API Gateway](./readme-api-gateway.md)

It is common that some of the Pulumi Output will need to be passed to the application for consumption. A typical example is the URL for the API Gateway endpoint. It is not know until after the deployment and the URL needs to be loaded by the frontend application for API invocation. We will use a minimum JavaScript app as a demo of how this is typically done.

## Preparing the mini frontend app
### Create a `publish` folder for loading into the s3 bucket
### Create a index.html and runtime-config.js file in the `publish` folder
    //runtime-config.js
    window['runtime-config'] = {
        apiUrl: 'http://localhost:8080/api'
    }

    //index.html
    <!DOCTYPE html>
    <html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
    <head>
      <meta charset="UTF-8" />
      <title>A JS Mini App</title>
      <script src="./runtime-config.js"></script>
      <script>
        function hitTheAPI() {
            var value = document.getElementById("my_input").value;
            const url = window["runtime-config"].apiUrl;
            alert(url)
        }
        </script>
    </head>
    <body>
      <h1>JS Mini App</h1>
      <input id="my_input" value="hello" />
      <button onclick="hitTheAPI()">Click Me</button>
    </html> 

### Deploy the folder to the s3 bucket
    var objects = bucket.BucketName.Apply(bucketName=>LoadFilesToS3(@"./public", bucketName));

    private static IEnumerable<BucketObject> LoadFilesToS3(string folderPath, string bucketName)
    {
        return Directory.EnumerateFiles(folderPath)
            .Select(file=>CreateBucketObject(file, bucketName))
            .ToList();
    }
    private static BucketObject CreateBucketObject (string filePath, string bucketName)
    {
        var fileName = Path.GetFileName(filePath);
        var fileExtension = Path.GetExtension(fileName);
        var s3Object = new BucketObject(fileName, new BucketObjectArgs
        {
            Bucket = bucketName,
            Source = new FileAsset(filePath),
            Key = fileName,
            ContentType = MimeMapping(fileExtension),

        });
        return s3Object;
    }
    private static string MimeMapping(string fileExtension) => fileExtension switch
    {
        ".htm" => "text/html",
        ".html" => "text/html",
        ".js" => "application/javascript",
        _ => throw new NotImplementedException($"Mime type for {fileExtension} is not defined"),
    };
    
## Overwrite runtime-config.js in the bucket
    var url = CreateRestAPIGatewayResources(apiGatewayLambda);
    var runtimeConfigJS = url.Apply(x=> new BucketObject("runtime-config.js", new BucketObjectArgs
    {
        Bucket = bucket.BucketName,
        Content = $@"window['runtime-config'] = {{apiUrl: '${x}'}}",
    }));

    //todo Duplicate resource URN, https://github.com/pulumi/pulumi/issues/5542
        
