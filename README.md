# FopFunctions

[Apache FOP](https://xmlgraphics.apache.org/fop/) is a powerful print formatter, commonly used to programmically render print output. FOP uses [XLS-FO](https://www.w3.org/TR/xsl11/) as its input and can produce PDF, PS, PCL, AFP, AWT and PNG as it's output. Being Java based, using it within a .NET application can be daunting task. This project is an attempt at a solution to aleviate this challenge by creating an [Azure function](https://azure.microsoft.com/en-us/services/functions/) to execute the FOP code in its native Java format, along with a .NET standard client that can easily be added to a .NET project.

I am not a Java developer so input from experts in the communinty is welcomed.

# Azure Functions (Java)

## Setting up Visual Studio Code

### Java development

The easiest way to set up Visual Studio Code for Java development is follow the instructions described [here](https://code.visualstudio.com/docs/java/java-tutorial).

### Azure Functions extension for Java.

Follow these [instructions](https://code.visualstudio.com/docs/java/java-azurefunctions) to set up a development enviroment that makes is easy to develop and test Azure Functions locally.

You may also need to set up a launch configuration. To do so, click the debug icon and either edit or create a new launch.json file. In the file, add the following;

```json
{
  "version": "0.2.0",
  "configurations": [
    -,
    {
      "name": "Attach to Java Functions",
      "type": "java",
      "request": "attach",
      "hostName": "127.0.0.1",
      "port": 5005,
      "preLaunchTask": "func: host start"
    },
    -
  ]
}
```

Now, debug using the function configuration you added above, and voila!

### Source code

The source code is found under the `azure_function` folder. You will have to add a `local.settings.json` file under the `azure_function` folder, if one doesn't already exist. The structure of the file should be similiar to this;

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobStorage": "",
    "FUNCTIONS_WORKER_RUNTIME": "java",
    "FOP_CONFIG_PATH": "[the absolute path to the fop.xconf file]",
    "FOP_FONT_CACHE_PATH": "[the absolute path to where the font cache file will be created]",
    "PASSWORD_KEY": "[the key to used to sign the HTTP request]"
  }
}
```

# FOP client (.NET)

Open `client/FopClient/FopClient.sln` in version of Visual Studio that supports .NET Standard 2.0 and the solution should build without any problems.
