using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Net.Browser;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Globalization;

namespace sdkMicrophoneCS.Tables
{
    public class CloudNoteClient : ICloudNoteClient
    {
        private const string MediaEndPoint = "/note";

        private readonly Uri cloudNoteServiceUri;
        private readonly Dispatcher dispatcher;

        public CloudNoteClient(Uri cloudNoteServiceUri)
            : this(cloudNoteServiceUri, null)
        {
        }

        public CloudNoteClient(Uri cloudNoteServiceUri, Dispatcher dispatcher)
        {
            if (cloudNoteServiceUri == null)
                throw new ArgumentNullException("cloudNoteServiceUri", "cloudNoteServiceUri cannot be null.");

            this.cloudNoteServiceUri = cloudNoteServiceUri;
            this.dispatcher = dispatcher;
        }

        public void AddCloudNote(CloudNote cloudNote, Action<CloudNoteServiceOperationResponse<bool>> callback)
        {
            if (cloudNote == null)
                throw new ArgumentNullException("cloudNote", "CloudNote cannot be null.");

            var requestUri = ConcatPath(this.cloudNoteServiceUri, MediaEndPoint, null);
            var request = this.ResolveRequest(requestUri);
            request.Method = "PUT";

            this.SendRequest(request, cloudNote, callback);
        }


        private static Uri ConcatPath(Uri uri, string path, string query)
        {
            var builder = new UriBuilder(uri);
            builder.Path = string.Concat(builder.Path.TrimEnd('/'), path);

            if (!string.IsNullOrWhiteSpace(query))
            {
                builder.Query = query;
            }

            return builder.Uri;
        }

        protected virtual HttpWebRequest ResolveRequest(Uri requestUri)
        {
            return (HttpWebRequest)WebRequestCreator.ClientHttp.Create(requestUri);
        }

        private void SendRequest(HttpWebRequest request, CloudNote cloudNote, Action<CloudNoteServiceOperationResponse<bool>> callback)
        {
            byte[] bodyContent;
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(CloudNote));
                serializer.WriteObject(stream, cloudNote);

                bodyContent = stream.ToArray();
            }

            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.BeginGetRequestStream(
                ar =>
                {
                    var postStream = request.EndGetRequestStream(ar);

                    postStream.Write(bodyContent, 0, bodyContent.Length);
                    postStream.Close();

                    request.BeginGetResponse(asyncResult => this.OnAddCloudNoteResponse(request, asyncResult, callback), request);
                },
            request);
        }

        private void OnAddCloudNoteResponse(HttpWebRequest request, IAsyncResult asyncResult, Action<CloudNoteServiceOperationResponse<bool>> callback)
        {
            try
            {
                var httpResponse = (HttpWebResponse)request.EndGetResponse(asyncResult);
                CloudNoteServiceOperationResponse<bool> mediaResponse;

                if (httpResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    mediaResponse = new CloudNoteServiceOperationResponse<bool>(
                        true,
                        httpResponse.StatusCode,
                        true,
                        string.Empty);
                }
                else
                {
                    mediaResponse = new CloudNoteServiceOperationResponse<bool>(
                        true,
                        httpResponse.StatusCode,
                        false,
                        "There was an error in the operation performed againts the Cloud Note service.");
                }

                this.DispatchCallback(callback, mediaResponse);
            }
            catch (WebException webException)
            {
                var response = (HttpWebResponse)webException.Response;
                this.DispatchCallback(callback, new CloudNoteServiceOperationResponse<bool>(false, response.StatusCode, false, Helper.ParseWebException(webException)));
            }
        }

        protected virtual void DispatchCallback<T>(Action<T> callback, T response)
        {
            if (callback != null)
            {
                if (this.dispatcher != null)
                    this.dispatcher.BeginInvoke(() => callback(response));
                else
                    callback(response);
            }
        }

       
    }
}
