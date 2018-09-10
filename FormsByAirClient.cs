using FormsByAir.SDK.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FormsByAir.SDK
{
    public class FormsByAirClient
    {
        private string ApiKey;
        private string EndPoint = "https://formsbyair.com";
        private RestClient client;

        public FormsByAirClient(string apiKey, string endPoint = null)
        {
            ApiKey = apiKey;
            client = new RestClient(endPoint ?? EndPoint);
            client.UserAgent = "FormsByAir Connect";
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

        public void Redeliver(string documentDeliveryId)
        {
            var request = new RestRequest("api/v1/documents/deliveries/{id}/redeliver", Method.PUT);
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

        public void LogDeliveryException(string documentDeliveryId, string message, string stackTrace)
        {
            var request = new RestRequest("api/v1/documents/deliveries/exceptions", Method.POST);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(new DeliveryException { DocumentDeliveryId = documentDeliveryId, Message = message, StackTrace = stackTrace });

            var response = client.Execute(request);

            if (response.ErrorException != null)
            {
                throw new Exception(response.ErrorException.Message);
            }
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception(response.StatusDescription);
            }
        }

        public void UpdateWorkflowStatus(string documentId, string workflowStatusId)
        {
            var request = new RestRequest("api/v1/documents/{id}/workflowstatus", Method.PUT);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddHeader("Content-Type", "application/json");
            request.AddUrlSegment("id", documentId);
            request.AddJsonBody(new Document { WorkflowStatusId = workflowStatusId });

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

            response.Data.Schema.Form.SetParent();
            return response.Data.Schema;
        }

        public List<Document> GetDocuments(string documentStatusId = null, string formId = null, int? currentPage = null, int? itemsPerPage = null)
        {
            var request = new RestRequest("api/v1/documents", Method.GET);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(documentStatusId))
            {
                request.AddQueryParameter("documentStatusId", documentStatusId);
            }
            if (!string.IsNullOrEmpty(formId))
            {
                request.AddQueryParameter("formId", formId);
            }
            if (currentPage.HasValue)
            {
                request.AddQueryParameter("currentPage", currentPage.Value.ToString());
            }
            if (itemsPerPage.HasValue)
            {
                request.AddQueryParameter("itemsPerPage", itemsPerPage.Value.ToString());
            }

            var response = client.Execute<List<Document>>(request);

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

        public Entity GetSubscriptionMap(string subscriptionId)
        {
            var request = new RestRequest("api/v1/subscriptions/{id}/map", Method.GET);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddHeader("Content-Type", "application/json");
            request.AddUrlSegment("id", subscriptionId);

            var response = client.Execute<Entity>(request);

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

        public FileDelivery GetFile(DocumentDelivery documentDelivery)
        {
            var request = new RestRequest("documents/deliveries/{id}", Method.GET);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddUrlSegment("id", documentDelivery.DocumentDeliveryId);

            var response = client.Execute(request);

            if (response.ErrorException != null)
            {
                throw new Exception(response.ErrorException.Message);
            }
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.StatusDescription);
            }

            var contentDisposition = response.Headers.SingleOrDefault(a => a.Name == "Content-Disposition").Value.ToString().Split(';');
            var filename = contentDisposition.Single(a => a.Contains("filename=")).Replace("filename=", "").Replace("\"", "").Trim();

            var fileDelivery = new FileDelivery();
            fileDelivery.FileName = filename;
            fileDelivery.File = response.RawBytes;
            fileDelivery.FilePath = documentDelivery.Subscription.FilePath;
            return fileDelivery;
        }

        public List<DocumentDelivery> GetFileDeliveriesForDocument(string documentId)
        {
            var request = new RestRequest("api/v1/documents/{id}/deliveries/files", Method.GET);
            request.AddHeader("Authorization", "Bearer " + ApiKey);
            request.AddHeader("Content-Type", "application/json");
            request.AddUrlSegment("id", documentId);

            var response = client.Execute<List<DocumentDelivery>>(request);

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
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception(response.StatusDescription);
            }

            return response.Data.DocumentId;
        }
    }
}
