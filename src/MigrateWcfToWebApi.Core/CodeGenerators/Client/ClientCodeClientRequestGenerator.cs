using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Client
{
    internal static class ClientCodeClientRequestGenerator
    {
        public static string CreateClientRequest(MethodDeclarationSyntax wcfClientMethod,
            MethodDeclarationSyntax serviceGenMethod, IEnumerable<MethodDeclarationSyntax> wcfServiceMethods)
        {
            var httpClientRequest = CreateHttpClientRequest(wcfClientMethod, serviceGenMethod, wcfServiceMethods);
            var startRequest = httpClientRequest.startRequest;
            var endRequest = httpClientRequest.endRequest;

            var returnType = wcfClientMethod.ReturnType.ToFullString();
            var responseType = CreateResponseType(returnType, endRequest);
            var clientRequest = $"{startRequest}\n\r{responseType}";

            return clientRequest;
        }

        private static (string startRequest, string endRequest) CreateHttpClientRequest(MethodDeclarationSyntax wcfClientMethod,
            MethodDeclarationSyntax serviceGenMethod, IEnumerable<MethodDeclarationSyntax> wcfServiceMethods)
        {
            var isHttpGet = serviceGenMethod
                .AttributeLists
                .SelectMany(list =>
                {
                    var attributeNames = list.Attributes.Select(b => b.Name.ToFullString());

                    return attributeNames;
                })
                .Any(name => name == "HttpGet");

            // endRequest will be used to set to `jsonResponse` variable later
            var httpGetRequest = (startRequest: "", endRequest: "_httpClient.GetStringAsync(requestUri).Result");
            var httpPostRequest = CreateHttpPostRequest(wcfClientMethod, serviceGenMethod, wcfServiceMethods);

            var request = isHttpGet
                ? httpGetRequest
                : httpPostRequest;

            return request;
        }

        private static (string startRequest, string endRequest) CreateHttpPostRequest(MethodDeclarationSyntax wcfClientMethod,
            MethodDeclarationSyntax serviceGenMethod, IEnumerable<MethodDeclarationSyntax> wcfServiceMethods)
        {
            var serviceParameters = serviceGenMethod.ParameterList.Parameters;
            var serviceComplexTypeParameter = ComplexTypesGenerator.FindComplexTypes(serviceParameters).SingleOrDefault();
            var serviceComplexTypeParameterName = serviceComplexTypeParameter?.Identifier.ValueText;

            // create anon class instance if method has two or more (i.e. "multiple") complex types parameters
            var multipleComplexTypes = ClientCodeMultipleComplexTypesGenerator.CreateMultipleComplexTypesClassInstance(wcfClientMethod, serviceComplexTypeParameterName, wcfServiceMethods);

            // should only be one complex type parmeter for any service method
            // hence serialize it to json for http post
            var jsonRequest = CreateJsonRequest(serviceComplexTypeParameter, serviceComplexTypeParameterName);

            // create http post request
            var startHttpPostRequest = $@"
    {multipleComplexTypes}
    var json = {jsonRequest};
    var content = new StringContent(json, Encoding.UTF8, ""application/json"");
    var response = _httpClient.PostAsync(requestUri, content).Result;
";

            var httpPostRequest = (startRequest: startHttpPostRequest, endRequest: "response.Content.ReadAsStringAsync().Result");

            return httpPostRequest;
        }

        private static string CreateJsonRequest(ParameterSyntax serviceComplexTypeParameter, string serviceComplexTypeParameterName)
        {
            var hasServiceComplexTypeParameter = serviceComplexTypeParameter != null;

            var jsonRequest = hasServiceComplexTypeParameter
                ? $"JsonConvert.SerializeObject({serviceComplexTypeParameterName})"
                : "string.Empty"; // if no complex type then include nothing in the request body (simple types are passed via url query string)

            return jsonRequest;
        }

        private static string CreateResponseType(string returnType, string endRequest)
        {
            var voidResponse = $@"
    var unused = {endRequest};
";

            var typeResponse = $@"
    var jsonResponse = {endRequest};

    var result = JsonConvert.DeserializeObject<{returnType}>(jsonResponse, _jsonSerializerSettings);

    return result;
";

            var isVoid = returnType.Trim() == "void";

            var responseType = isVoid
                ? voidResponse
                : typeResponse;

            return responseType;
        }
    }
}