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
  // todo
## Passing the Pulumi Output into the frontend app
  // todo
### Overwriting rumtime-config.js during deployment
  // todo

