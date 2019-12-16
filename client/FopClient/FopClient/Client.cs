using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Flexerant.FopClient
{
    public class Client : IDisposable
    {
        private readonly string _enpointUrl;
        private readonly string _secret;
        private readonly HttpClient _httpClient;

        public Client(string endpointUrl, string secret) : this(endpointUrl, secret, null) { }

        public Client(string endpointUrl, string secret, HttpMessageHandler httpMessageHandler)
        {
            _enpointUrl = endpointUrl;
            _secret = secret;

            if (httpMessageHandler == null)
            {
                _httpClient = new HttpClient();
            }
            else
            {
                _httpClient = new HttpClient(httpMessageHandler);
            }
        }

        public async Task ConvertAsync(Stream inputStream, Stream outputStream)
        {
            inputStream.Position = 0;

            if (!this.IsXml(inputStream)) throw new XmlException("The provided stream is not valid XML.");

            string sig = this.CreateSignature(inputStream, _secret);
            string url = $"{_enpointUrl}?sig={sig}";

            StreamContent streamContent = new StreamContent(inputStream);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            HttpResponseMessage response = await _httpClient.PostAsync(url, streamContent);

            if (!response.IsSuccessStatusCode) throw new ResponseException(await response.Content.ReadAsStringAsync());

            Stream responseStream = await response.Content.ReadAsStreamAsync();

            await responseStream.CopyToAsync(outputStream);

        }

        // TODO: Make this schema aware.
        private bool IsXml(Stream stream)
        {
            bool isXml = false;

            XmlReaderSettings settings = new XmlReaderSettings()
            {
                CheckCharacters = true,
                ConformanceLevel = ConformanceLevel.Document,
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                ValidationFlags = XmlSchemaValidationFlags.None,
                ValidationType = ValidationType.Schema
            };

            void FopValidationEventHandler(object sender, ValidationEventArgs e)
            {
                if (e.Severity == XmlSeverityType.Error) isXml = false;
            }

            settings.ValidationEventHandler += FopValidationEventHandler;

            try
            {
                using (XmlReader fop = XmlReader.Create(stream, settings))
                {
                    while (fop.Read()) { }
                }

                isXml = true;
            }
            catch
            {
                isXml = false;
            }
            finally
            {
                settings.ValidationEventHandler -= FopValidationEventHandler;
                stream.Position = 0;
            }

            return isXml;
        }

        private string CreateSignature(Stream stream, string secret)
        {
            secret = secret ?? "";
            var encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(secret);

            using (var hmac = new HMACSHA1(keyByte))
            {
                byte[] hashmessage = hmac.ComputeHash(stream);

                stream.Position = 0;
                return string.Concat(hashmessage.Select(b => b.ToString("x2")));
            }
        }

        private string CreateSignature(byte[] body, string secret)
        {
            secret = secret ?? "";
            var encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(secret);

            using (var hmac = new HMACSHA1(keyByte))
            {
                byte[] hashmessage = hmac.ComputeHash(body);

                return string.Concat(hashmessage.Select(b => b.ToString("x2")));
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
