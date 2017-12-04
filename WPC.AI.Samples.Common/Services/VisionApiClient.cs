namespace WPC.AI.Samples.Common
{
    using System;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.ProjectOxford.Vision.Contract;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.ProjectOxford.Vision;
    using System.Net.Http;
    using System.Threading;

    /// <summary>
    /// The vision service client.
    /// </summary>
    public class VisionApiClient : IVisionServiceClient
    {
        private HttpClient m_HttpClient;

        /// <summary>
        /// The service host
        /// </summary>
        private const string DEFAULT_API_ROOT = "https://westcentralus.api.cognitive.microsoft.com/vision/v1.0";

        /// <summary>
        /// Host root, overridable by subclasses, intended for testing.
        /// </summary>
        protected virtual string ServiceHost => _apiRoot;

        /// <summary>
        /// Default timeout for calls
        /// </summary>
        private const int DEFAULT_TIMEOUT = 2 * 60 * 1000; // 2 minutes timeout

        /// <summary>
        /// Default timeout for calls, overridable by subclasses
        /// </summary>
        protected virtual int DefaultTimeout => DEFAULT_TIMEOUT;

        /// <summary>
        /// The analyze query
        /// </summary>
        private const string AnalyzeQuery = "analyze";

        /// <summary>
        /// The describe query
        /// </summary>
        private const string DescribeQuery = "describe";

        /// <summary>
        /// The models-based query path part
        /// </summary>
        private const string ModelsPart = "models";

        /// <summary>
        /// The generate thumbnails query
        /// </summary>
        private const string ThumbnailsQuery = "generateThumbnail";

        /// <summary>
        /// Query parameter for maximum description candidates.
        /// </summary>
        private const string _maxCandidatesName = "maxCandidates";

        /// <summary>
        /// The subscription key name
        /// </summary>
        private const string _subscriptionKeyName = "subscription-key";

        /// <summary>
        /// The default resolver
        /// </summary>
        private CamelCasePropertyNamesContractResolver _defaultResolver = new CamelCasePropertyNamesContractResolver();

        /// <summary>
        /// The subscription key
        /// </summary>
        private string _subscriptionKey;

        /// <summary>
        /// The root URI for Vision API
        /// </summary>
        private readonly string _apiRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisionServiceClient"/> class.
        /// </summary>
        /// <param name="subscriptionKey">The subscription key.</param>
        public VisionApiClient(string subscriptionKey) : this(subscriptionKey, DEFAULT_API_ROOT) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisionServiceClient"/> class.
        /// </summary>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <param name="apiRoot">Root URI for the service endpoint.</param>
        public VisionApiClient(string subscriptionKey, string apiRoot)
        {
            _apiRoot = apiRoot?.TrimEnd('/');
            _subscriptionKey = subscriptionKey;

            m_HttpClient = new HttpClient();
        }

        /// <summary>
        /// Analyzes the image.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="visualFeatures">The visual features.</param>
        /// <param name="details">Optional domain-specific models to invoke when appropriate.  To obtain names of models supported, invoke the <see cref="ListModelsAsync">ListModelsAsync</see> method.</param>
        /// <returns>The AnalysisResult object.</returns>
        public async Task<AnalysisResult> AnalyzeImageAsync(string url, IEnumerable<VisualFeature> visualFeatures = null, IEnumerable<string> details = null)
        {
            //dynamic request = new ExpandoObject();
            //request.url = url;

            return await AnalyzeImageAsync<object>(new { url = url }, visualFeatures, details).ConfigureAwait(false);
        }

        /// <summary>
        /// Analyzes the image.
        /// </summary>
        /// <param name="imageStream">The image stream.</param>
        /// <param name="visualFeatures">The visual features.</param>
        /// <param name="details">Optional domain-specific models to invoke when appropriate.  To obtain names of models supported, invoke the <see cref="ListModelsAsync">ListModelsAsync</see> method.</param>
        /// <returns>The AnalysisResult object.</returns>
        public async Task<AnalysisResult> AnalyzeImageAsync(Stream imageStream, IEnumerable<VisualFeature> visualFeatures = null, IEnumerable<string> details = null)
        {
            return await AnalyzeImageAsync<Stream>(imageStream, visualFeatures, details).ConfigureAwait(false);
        }

        /// <summary>
        /// Analyzes the image.
        /// </summary>
        /// <param name="body">Body </param>
        /// <param name="visualFeatures">The visual features.</param>
        /// <param name="details">Optional domain-specific models to invoke when appropriate.  To obtain names of models supported, invoke the <see cref="ListModelsAsync">ListModelsAsync</see> method.</param>
        /// <returns>The AnalysisResult object.</returns>
        private async Task<AnalysisResult> AnalyzeImageAsync<T>(T body, IEnumerable<VisualFeature> visualFeatures, IEnumerable<string> details)
        {
            var requestUrl = new StringBuilder(ServiceHost).Append('/').Append(AnalyzeQuery).Append("?");
            requestUrl.Append(string.Join("&", new List<string>
            {
                VisualFeaturesToString(visualFeatures),
                DetailsToString(details),
                _subscriptionKeyName + "=" + _subscriptionKey
            }
            .Where(s => !string.IsNullOrEmpty(s))));
            
            return await this.SendAsync<T, AnalysisResult>(requestUrl.ToString(), body).ConfigureAwait(false);
        }

        /// <summary>
        /// Analyzes the image using a domain-specific model.
        /// </summary>
        /// <param name="url">The image URL.</param>
        /// <param name="model">Domain-specific model.</param>
        /// <remarks>The list of currently aailable models can be listed via the <see cref="ListModelsAsync"/> method.</remarks>
        /// <returns>The AnalysisInDomainResult object.</returns>
        public async Task<AnalysisInDomainResult> AnalyzeImageInDomainAsync(string url, Microsoft.ProjectOxford.Vision.Contract.Model model)
        {
            return await AnalyzeImageInDomainAsync(url, model.Name).ConfigureAwait(false);
        }

        /// <summary>
        /// Analyzes the image using a domain-specific model.
        /// </summary>
        /// <param name="imageStream">The image stream.</param>
        /// <param name="model">Domain-specific model.</param>
        /// <remarks>The list of currently aailable models can be listed via the <see cref="ListModelsAsync"/> method.</remarks>
        /// <returns>The AnalysisInDomainResult object.</returns>
        public async Task<AnalysisInDomainResult> AnalyzeImageInDomainAsync(Stream imageStream, Microsoft.ProjectOxford.Vision.Contract.Model model)
        {
            return await AnalyzeImageInDomainAsync(imageStream, model.Name).ConfigureAwait(false);
        }

        /// <summary>
        /// Analyzes the image using a domain-specific model.
        /// </summary>
        /// <param name="url">The image URL.</param>
        /// <param name="modelName">Name of the domain-specific model.</param>
        /// <remarks>The list of currently aailable models can be listed via the <see cref="ListModelsAsync"/> method.</remarks>
        /// <returns>The AnalysisInDomainResult object.</returns>
        public async Task<AnalysisInDomainResult> AnalyzeImageInDomainAsync(string url, string modelName)
        {
            string requestUrl = string.Format("{0}/{1}/{2}/{3}?{4}={5}", ServiceHost, ModelsPart, modelName, AnalyzeQuery, _subscriptionKeyName, _subscriptionKey);

            dynamic requestObject = new ExpandoObject();
            requestObject.url = url;

            return await this.SendAsync<ExpandoObject, AnalysisInDomainResult>(requestUrl, requestObject).ConfigureAwait(false);
        }

        /// <summary>
        /// Analyzes the image using a domain-specific model.
        /// </summary>
        /// <param name="imageStream">The image stream.</param>
        /// <param name="modelName">Name of the domain-specific model.</param>
        /// <remarks>The list of currently aailable models can be listed via the <see cref="ListModelsAsync"/> method.</remarks>
        /// <returns>The AnalysisInDomainResult object.</returns>
        public async Task<AnalysisInDomainResult> AnalyzeImageInDomainAsync(Stream imageStream, string modelName)
        {
            string requestUrl = string.Format("{0}/{1}/{2}/{3}?{4}={5}", ServiceHost, ModelsPart, modelName, AnalyzeQuery, _subscriptionKeyName, _subscriptionKey);
            return await this.SendAsync<Stream, AnalysisInDomainResult>(requestUrl, imageStream).ConfigureAwait(false);
        }

        /// <summary>
        /// Lists the domain-specific image-analysis models.
        /// </summary>
        /// <returns>An ModelResult, containing an array of Model objects.</returns>
        public async Task<ModelResult> ListModelsAsync()
        {
            string requestUrl = string.Format("{0}/{1}?{2}={3}", ServiceHost, ModelsPart, _subscriptionKeyName, _subscriptionKey);
            return await this.GetAsync<ModelResult>(requestUrl).ConfigureAwait(false);
        }
        /// <summary>
        /// 
        /// Gets the description of an image.
        /// </summary>
        /// <param name="url">The image URL.</param>
        /// <param name="maxCandidates">Maximum number of candidates to return.  Defaults to 1.</param>
        /// <returns>A DescribeResult object.</returns>
        public async Task<AnalysisResult> DescribeAsync(string url, int maxCandidates = 1)
        {
            string requestUrl = string.Format("{0}/{1}?{2}={3}&{4}={5}", ServiceHost, DescribeQuery, _maxCandidatesName, maxCandidates, _subscriptionKeyName, _subscriptionKey);

            dynamic requestObject = new ExpandoObject();
            requestObject.url = url;

            return await this.SendAsync<ExpandoObject, AnalysisResult>(requestUrl, requestObject).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the description of an image.
        /// </summary>
        /// <param name="imageStream">The image stream.</param>
        /// <param name="maxCandidates">Maximum number of candidates to return.  Defaults to 1.</param>
        /// <returns>A DescribeResult object.</returns>
        public async Task<AnalysisResult> DescribeAsync(Stream imageStream, int maxCandidates = 1)
        {
            string requestUrl = string.Format("{0}/{1}?{2}={3}&{4}={5}", ServiceHost, DescribeQuery, _maxCandidatesName, maxCandidates, _subscriptionKeyName, _subscriptionKey);
            return await this.SendAsync<Stream, AnalysisResult>(requestUrl, imageStream).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the thumbnail.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="smartCropping">if set to <c>true</c> [smart cropping].</param>
        /// <returns>Image bytes</returns>
        public async Task<byte[]> GetThumbnailAsync(string url, int width, int height, bool smartCropping = true)
        {
            string requestUrl = string.Format("{0}/{1}?width={2}&height={3}&smartCropping={4}&{5}={6}", ServiceHost, ThumbnailsQuery, width, height, smartCropping, _subscriptionKeyName, _subscriptionKey);
            
            dynamic requestObject = new ExpandoObject();
            requestObject.url = url;

            return await this.SendAsync<ExpandoObject, byte[]>(requestUrl, requestObject).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the thumbnail.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="smartCropping">if set to <c>true</c> [smart cropping].</param>
        /// <returns>Image bytes</returns>
        public async Task<byte[]> GetThumbnailAsync(Stream stream, int width, int height, bool smartCropping = true)
        {
            string requestUrl = string.Format("{0}/{1}?width={2}&height={3}&smartCropping={4}&{5}={6}", ServiceHost, ThumbnailsQuery, width, height, smartCropping, _subscriptionKeyName, _subscriptionKey);
            return await this.SendAsync<Stream, byte[]>(requestUrl, stream).ConfigureAwait(false);
        }

        /// <summary>
        /// Recognizes the text asynchronous.
        /// </summary>
        /// <param name="imageUrl">The image URL.</param>
        /// <param name="languageCode">The language code.</param>
        /// <param name="detectOrientation">if set to <c>true</c> [detect orientation].</param>
        /// <returns>The OCR object.</returns>
        public async Task<OcrResults> RecognizeTextAsync(string imageUrl, string languageCode = LanguageCodes.AutoDetect, bool detectOrientation = true)
        {
            string requestUrl = string.Format("{0}/ocr?language={1}&detectOrientation={2}&{3}={4}", ServiceHost, languageCode, detectOrientation, _subscriptionKeyName, _subscriptionKey);

            dynamic requestObject = new ExpandoObject();
            requestObject.url = imageUrl;

            return await this.SendAsync<ExpandoObject, OcrResults>(requestUrl, requestObject).ConfigureAwait(false);
        }

        /// <summary>
        /// Recognizes the text asynchronous.
        /// </summary>
        /// <param name="imageStream">The image stream.</param>
        /// <param name="languageCode">The language code.</param>
        /// <param name="detectOrientation">if set to <c>true</c> [detect orientation].</param>
        /// <returns>The OCR object.</returns>
        public async Task<OcrResults> RecognizeTextAsync(Stream imageStream, string languageCode = LanguageCodes.AutoDetect, bool detectOrientation = true)
        {
            string requestUrl = string.Format("{0}/ocr?language={1}&detectOrientation={2}&{3}={4}", ServiceHost, languageCode, detectOrientation, _subscriptionKeyName, _subscriptionKey);

            return await this.SendAsync<Stream, OcrResults>(requestUrl, imageStream).ConfigureAwait(false);
        }

        /// <summary>
        /// Recognizes the handwriting text asynchronous.
        /// </summary>
        /// <param name="imageUrl">The image URL.</param>
        /// <returns>HandwritingRecognitionOperation created</returns>
        public async Task<HandwritingRecognitionOperation> CreateHandwritingRecognitionOperationAsync(string imageUrl)
        {
            string requestUrl = string.Format("{0}/recognizeText?handwriting=true&{1}={2}", ServiceHost, _subscriptionKeyName, _subscriptionKey);

            dynamic requestObject = new ExpandoObject();
            requestObject.url = imageUrl;

            return await this.SendAsync<ExpandoObject, HandwritingRecognitionOperation>(requestUrl, requestObject).ConfigureAwait(false);
        }

        /// <summary>
        /// Recognizes the handwriting text asynchronous.
        /// </summary>
        /// <param name="imageStream">The image stream.</param>
        /// <returns>HandwritingRecognitionOperation created</returns>
        public async Task<HandwritingRecognitionOperation> CreateHandwritingRecognitionOperationAsync(Stream imageStream)
        {
            string requestUrl = string.Format("{0}/recognizeText?handwriting=true&{1}={2}", ServiceHost, _subscriptionKeyName, _subscriptionKey);
            return await this.SendAsync<Stream, HandwritingRecognitionOperation>(requestUrl, imageStream).ConfigureAwait(false);
        }

        /// <summary>
        /// Get HandwritingRecognitionOperationResult
        /// </summary>
        /// <param name="opeartion">HandwritingRecognitionOperation object</param>
        /// <returns>HandwritingRecognitionOperationResult</returns>
        public async Task<HandwritingRecognitionOperationResult> GetHandwritingRecognitionOperationResultAsync(HandwritingRecognitionOperation opeartion)
        {
            string requestUrl = string.Format("{0}?{1}={2}", opeartion.Url, _subscriptionKeyName, _subscriptionKey);
            return await this.GetAsync<HandwritingRecognitionOperationResult>(requestUrl).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the tags associated with an image.
        /// </summary>
        /// <param name="imageStream">The image stream.</param>
        /// <returns>Analysis result with tags.</returns>
        public async Task<AnalysisResult> GetTagsAsync(Stream imageStream)
        {
            string requestUrl = string.Format("{0}/tag?{1}={2}", ServiceHost, _subscriptionKeyName, _subscriptionKey);
            return await this.SendAsync<Stream, AnalysisResult>(requestUrl, imageStream).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the tags associated with an image.
        /// </summary>
        /// <param name="imageUrl">The image URL.</param>
        /// <returns>Analysis result with tags.</returns>
        public async Task<AnalysisResult> GetTagsAsync(string imageUrl)
        {
            string requestUrl = string.Format("{0}/tag?{1}={2}", ServiceHost, _subscriptionKeyName, _subscriptionKey);
            
            dynamic requestObject = new ExpandoObject();
            requestObject.url = imageUrl;

            return await this.SendAsync<ExpandoObject, AnalysisResult>(requestUrl, requestObject).ConfigureAwait(false);
        }

        /// <summary>
        /// Strings the array to string.
        /// </summary>
        /// <param name="features">String array of feature names.</param>
        /// <returns>The visual features query parameter string.</returns>
        private string VisualFeaturesToString(string[] features)
        {
            return (features == null || features.Length == 0)
                ? ""
                : "visualFeatures=" + string.Join(",", features);
        }

        /// <summary>
        /// Variable-length VisualFeatures arguments to a string.
        /// </summary>
        /// <param name="features">Variable-length VisualFeatures.</param>
        /// <returns>The visual features query parameter string.</returns>
        private string VisualFeaturesToString(IEnumerable<VisualFeature> features)
        {
            return VisualFeaturesToString(features?.Select(feature => feature.ToString()).ToArray());
        }

        /// <summary>
        /// Strings the array to string.
        /// </summary>
        /// <param name="features">String array of feature names.</param>
        /// <returns>The visual features query parameter string.</returns>
        private string DetailsToString(IEnumerable<string> details)
        {
            return (details == null || details.Count() == 0)
                ? ""
                : "details=" + string.Join(",", details);
        }

        #region the json client

        /// <summary>
        /// Gets the asynchronous.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="method">The method.</param>
        /// <param name="request">The request.</param>
        /// <param name="setHeadersCallback">The set headers callback.</param>
        /// <returns>
        /// The response object.
        /// </returns>
        private async Task<TResponse> GetAsync<TResponse>(string requestUrl, Action<WebRequest> setHeadersCallback = null)
        {
            try
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                Task<HttpResponseMessage> requestTask = m_HttpClient.GetAsync(requestUrl, tokenSource.Token);
                
                var resultTask = await Task.WhenAny(requestTask, Task.Delay(DefaultTimeout)).ConfigureAwait(false);

                return await this.ProcessAsyncResponse<TResponse>(requestTask.Result);
            }
            catch (AggregateException ae)
            {
                ae.Handle(e =>
                {
                    this.HandleException(e);
                    return true;
                });
                return default(TResponse);
            }
            catch (Exception e)
            {
                this.HandleException(e);
                return default(TResponse);
            }
        }

        /// <summary>
        /// Sends the asynchronous.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="method">The method.</param>
        /// <param name="requestBody">The request body.</param>
        /// <param name="request">The request.</param>
        /// <param name="setHeadersCallback">The set headers callback.</param>
        /// <returns>The response object.</returns>
        /// <exception cref="System.ArgumentNullException">request</exception>
        private async Task<TResponse> SendAsync<TRequest, TResponse>(string requestUrl, TRequest requestBody, Action<HttpClient> setHeadersCallback = null)
        {
            try
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                Task<HttpResponseMessage> requestTask;
                if (requestBody is Stream)
                {
                    requestTask = m_HttpClient.PostAsync(requestUrl, new StreamContent(requestBody as Stream), tokenSource.Token);
                }
                else
                {
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody));
                    jsonContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    requestTask = m_HttpClient.PostAsync(requestUrl, jsonContent, tokenSource.Token);
                }

                var resultTask = await Task.WhenAny(requestTask, Task.Delay(DefaultTimeout)).ConfigureAwait(false);

                // Abort request if timeout has expired
                if (!requestTask.IsCompleted)
                {
                    tokenSource.Cancel();
                }

                return await this.ProcessAsyncResponse<TResponse>(requestTask.Result);
            }
            catch (AggregateException ae)
            {
                ae.Handle(e =>
                {
                    this.HandleException(e);
                    return true;
                });
                return default(TResponse);
            }
            catch (Exception e)
            {
                this.HandleException(e);
                return default(TResponse);
            }
        }

        /// <summary>
        /// Processes the asynchronous response.
        /// </summary>
        /// <typeparam name="T">Type of response.</typeparam>
        /// <param name="webResponse">The web response.</param>
        /// <returns>The response.</returns>
        private async Task<T> ProcessAsyncResponse<T>(HttpResponseMessage webResponse)
        {
            using (webResponse)
            {
                if (webResponse.StatusCode == HttpStatusCode.OK ||
                    webResponse.StatusCode == HttpStatusCode.Accepted ||
                    webResponse.StatusCode == HttpStatusCode.Created)
                {
                    if (webResponse.Content.Headers.ContentLength != 0)
                    {
                        using (var stream = await webResponse.Content.ReadAsStreamAsync())
                        {
                            if (stream != null)
                            {
                                if (webResponse.Content.Headers.ContentType.MediaType ==  "image/jpeg" ||
                                    webResponse.Content.Headers.ContentType.MediaType == "image/png")
                                {
                                    using (MemoryStream ms = new MemoryStream())
                                    {
                                        stream.CopyTo(ms);
                                        return (T)(object)ms.ToArray();
                                    }
                                }
                                else
                                {
                                    string message = string.Empty;
                                    using (StreamReader reader = new StreamReader(stream))
                                    {
                                        message = reader.ReadToEnd();
                                    }

                                    JsonSerializerSettings settings = new JsonSerializerSettings();
                                    settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                                    settings.NullValueHandling = NullValueHandling.Ignore;
                                    settings.ContractResolver = this._defaultResolver;

                                    return JsonConvert.DeserializeObject<T>(message, settings);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (webResponse.Headers.Contains("Operation-Location"))
                        {
                            string message = string.Format("{{Url: \"{0}\"}}", webResponse.Headers.First(h => h.Key == "Operation-Location"));

                            JsonSerializerSettings settings = new JsonSerializerSettings();
                            settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                            settings.NullValueHandling = NullValueHandling.Ignore;
                            settings.ContractResolver = this._defaultResolver;

                            return JsonConvert.DeserializeObject<T>(message, settings);
                        }
                    }
                }
            }

            return default(T);
        }

        /// <summary>
        /// Set request content type.
        /// </summary>
        /// <param name="request">Web request object.</param>
        private void SetCommonHeaders(WebRequest request)
        {
            request.ContentType = "application/json";
            //request.Headers[HttpRequestHeader.Authorization] = "Basic ZTkwNTE2ZmQ4NThlNDVjMmFhNDMzMjRlZjBlOThlN2E=";
        }

        /// <summary>
        /// Serialize the request body to byte array.
        /// </summary>
        /// <typeparam name="T">Type of request object.</typeparam>
        /// <param name="requestBody">Strong typed request object.</param>
        /// <returns>Byte array.</returns>
        private byte[] SerializeRequestBody<T>(T requestBody)
        {
            if (requestBody == null || requestBody is Stream)
            {
                return null;
            }
            else
            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                settings.ContractResolver = this._defaultResolver;

                return System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestBody, settings));
            }
        }

        /// <summary>
        /// Process the exception happened on rest call.
        /// </summary>
        /// <param name="exception">Exception object.</param>
        private void HandleException(Exception exception)
        {
            WebException webException = exception as WebException;
            if (webException != null && webException.Response != null)
            {
                if (webException.Response.ContentType.ToLower().Contains("application/json"))
                {
                    Stream stream = null;

                    try
                    {
                        stream = webException.Response.GetResponseStream();
                        if (stream != null)
                        {
                            string errorObjectString;
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                stream = null;
                                errorObjectString = reader.ReadToEnd();
                            }

                            ClientError errorCollection = JsonConvert.DeserializeObject<ClientError>(errorObjectString);

                            // HandwritingOcr error message use the latest format, so add the logic to handle this issue.
                            if (errorCollection.Code == null && errorCollection.Message == null)
                            {
                                var errorType = new { Error = new ClientError() };
                                var errorObj = JsonConvert.DeserializeAnonymousType(errorObjectString, errorType);
                                errorCollection = errorObj.Error;
                            }

                            if (errorCollection != null)
                            {
                                throw new ClientException
                                {
                                    Error = errorCollection,
                                };
                            }
                        }
                    }
                    finally
                    {
                        if (stream != null)
                        {
                            stream.Dispose();
                        }
                    }
                }
            }

            throw exception;
        }

        /// <summary>
        /// This class is used to pass on "state" between each Begin/End call
        /// It also carries the user supplied "state" object all the way till
        /// the end where is then hands off the state object to the
        /// WebRequestCallbackState object.
        /// </summary>
        internal class WebRequestAsyncState
        {
            /// <summary>
            /// Gets or sets request bytes of the request parameter for http post.
            /// </summary>
            public byte[] RequestBytes { get; set; }

            /// <summary>
            /// Gets or sets the HttpWebRequest object.
            /// </summary>
            public HttpWebRequest WebRequest { get; set; }

            /// <summary>
            /// Gets or sets the request state object.
            /// </summary>
            public object State { get; set; }
        }

        #endregion
    }
}
