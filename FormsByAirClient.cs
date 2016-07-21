using FormsByAir.SDK.Model;
using RestSharp;
using System;
using System.Collections.Generic;

namespace FormsByAir.SDK
{
    public class FormsByAirClient
    {
        private string ApiKey;
        private string EndPoint = "https://formsbyair.com";
        private RestClient client;

        public FormsByAirClient(string apiKey)
        {
            ApiKey = apiKey;
            client = new RestClient(EndPoint);
        }

        public void SetDelivered(string documentDeliveryId)
        {
            var request = new RestRequest("api/v1/documents/deliveries/{id}/delivered", Method.PUT);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddHeader("Content-Type", "application/json");
            request.AddUrlSegment("id", documentDeliveryId);

            var response = client.Execute(request);

            if (response.ErrorException != null)
            {
                throw new Exception(response.ErrorException.Message);
            }
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.StatusDescription);
            }
        }

        public Schema GetDocument(string documentId)
        {
            var request = new RestRequest("api/v1/documents/{id}", Method.GET);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddHeader("Content-Type", "application/json");
            request.AddUrlSegment("id", documentId);

            var response = client.Execute<DocumentResponse>(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            if (response.ErrorException != null)
            {
                throw new Exception(response.ErrorException.Message);
            }
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.StatusDescription);
            }

            return response.Data.Schema;
        }

        public byte[] GetFileDelivery(string documentDeliveryId)
        {
            var request = new RestRequest("documents/deliveries/{id}", Method.GET);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddUrlSegment("id", documentDeliveryId);

            var response = client.Execute(request);

            if (response.ErrorException != null)
            {
                throw new Exception(response.ErrorException.Message);
            }
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.StatusDescription);
            }

            return response.RawBytes;
        }

        public DocumentDelivery GetFileDeliveryForDocument(string documentId)
        {
            var request = new RestRequest("api/v1/documents/{id}/deliveries/file", Method.GET);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddHeader("Content-Type", "application/json");
            request.AddUrlSegment("id", documentId);

            var response = client.Execute<DocumentDelivery>(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            if (response.ErrorException != null)
            {
                throw new Exception(response.ErrorException.Message);
            }
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.StatusDescription);
            }

            return response.Data;
        }

        public List<DocumentDelivery> GetPendingDocumentDeliveries()
        {
            var request = new RestRequest("api/v1/documents/deliveries/pending", Method.GET);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddHeader("Content-Type", "application/json");

            var response = client.Execute<List<DocumentDelivery>>(request);

            if (response.ErrorException != null)
            {
                throw new Exception(response.ErrorException.Message);
            }
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.StatusDescription);
            }

            return response.Data;
        }

        public string GenerateRequest(DocumentRequest documentRequest)
        {
            var request = new RestRequest("api/v1/documents/request", Method.POST);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(documentRequest);

            var response = client.Execute<DocumentRequestResponse>(request);

            if (response.ErrorException != null)
            {
                throw new Exception(response.ErrorException.Message);
            }
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.StatusDescription);
            }

            return response.Data.DocumentId;
        }
    }
}
