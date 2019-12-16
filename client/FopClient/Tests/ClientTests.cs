using Flexerant.FopClient;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace Tests
{
    public class ClientTests
    {
        [Fact(Skip = "This text needs to be run manually.")]
        public void Test1()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                .Build();

            string secret = config["PASSWORD_KEY"];
            string outputPath = config["OUTPUT_PATH"];
            string url = config["PROD_URL"];

            using (Client client = new Client(url, secret))
            {
                using (MemoryStream ms = new MemoryStream(Properties.Resources.alignment))
                {
                    using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Write))
                    {
                        client.ConvertAsync(ms, fs).Wait();
                    }
                }
            }
        }

        [Fact]
        public void GoodXml()
        {
            string secret = Guid.NewGuid().ToString();
            string url = "https://test.com";
            byte[] responseData;
            Mock<HttpMessageHandler> handlerMock = this.GetSuccessMockHttpMessageHandler();

            using (Client client = new Client(url, secret, handlerMock.Object))
            {
                using (MemoryStream msIn = new MemoryStream(Properties.Resources.alignment))
                {
                    using (MemoryStream msOut = new MemoryStream())
                    {
                        client.ConvertAsync(msIn, msOut).Wait();

                        responseData = msOut.ToArray();
                    }
                }
            }

            Assert.Equal(16, responseData.Length);
        }

        [Fact]
        public void BadXml()
        {
            string secret = Guid.NewGuid().ToString();
            string url = "https://test.com";
            Mock<HttpMessageHandler> handlerMock = this.GetSuccessMockHttpMessageHandler();

            using (Client client = new Client(url, secret, handlerMock.Object))
            {
                Assert.ThrowsAsync<XmlException>(async () =>
                {
                    using (MemoryStream msIn = new MemoryStream(Guid.NewGuid().ToByteArray()))
                    {
                        using (MemoryStream msOut = new MemoryStream())
                        {
                            await client.ConvertAsync(msIn, msOut);
                        }
                    }
                });
            }
        }

        [Fact]
        public void BadResponse()
        {
            string secret = Guid.NewGuid().ToString();
            string url = "https://test.com";
            Mock<HttpMessageHandler> handlerMock = this.GetErrorMockHttpMessageHandler();

            using (Client client = new Client(url, secret, handlerMock.Object))
            {
                Assert.ThrowsAsync<ResponseException>(async () =>
                {
                    using (MemoryStream msIn = new MemoryStream(Properties.Resources.alignment))
                    {
                        using (MemoryStream msOut = new MemoryStream())
                        {
                            await client.ConvertAsync(msIn, msOut);
                        }
                    }
                });
            }
        }

        private Mock<HttpMessageHandler> GetSuccessMockHttpMessageHandler()
        {
            // ARRANGE
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
               .Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new ByteArrayContent(Guid.NewGuid().ToByteArray()),
               })
               .Verifiable();

            return handlerMock;
        }

        private Mock<HttpMessageHandler> GetErrorMockHttpMessageHandler()
        {
            // ARRANGE
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
               .Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.InternalServerError
               })
               .Verifiable();

            return handlerMock;
        }
    }
}
