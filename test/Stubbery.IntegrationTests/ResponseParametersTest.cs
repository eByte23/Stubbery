﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Stubbery.IntegrationTests
{
    public class ResponseParametersTest
    {
        private readonly HttpClient httpClient = new HttpClient();

        [Fact]
        public void ResponseBody_NullPassed_ArgumentNullException()
        {
            using (var sut = new ApiStub())
            {
                Assert.Throws<ArgumentNullException>(() => sut.Get().Response(null));
            }
        }

        [Fact]
        public void ResponseBody_CalledTwice_InvalidOperationException()
        {
            using (var sut = new ApiStub())
            {
                Assert.Throws<InvalidOperationException>(
                    () => sut.Get().Response((req, args) => "1").Response((req, args) => "2"));
            }
        }

        [Fact]
        public async Task ResponseBody_BodySet_BodyReturned_StringResponse()
        {
            using (var sut = new ApiStub())
            {
                sut.Get("/testget", (req, args) => "testresponse");

                sut.Start();

                var result = await httpClient.GetAsync(new UriBuilder(new Uri(sut.Address)) { Path = "/testget" }.Uri);

                var resultString = await result.Content.ReadAsStringAsync();

                Assert.Equal("testresponse", resultString);
            }
        }

        [Fact]
        public async Task ResponseBody_BodySet_BodyReturned_ActionResultResponse()
        {
            using (var sut = new ApiStub())
            using (var ms = new MemoryStream())
            {
                await ms.WriteAsync(Encoding.UTF8.GetBytes("Test File Data"));
                ms.Position = 0;

                sut.Get("/testget", (req, args) => new FileStreamResult(ms, "text/plain"));

                sut.Start();

                var result = await httpClient.GetAsync(new UriBuilder(new Uri(sut.Address)) { Path = "/testget" }.Uri);

                var resultContentType = result.Content.Headers.ContentType.MediaType;
                var resultFileData = await result.Content.ReadAsStringAsync();

                Assert.Equal("text/plain", resultContentType);
                Assert.Equal("Test File Data", resultFileData);
            }
        }

        [Fact]
        public async Task ResponseBody_BodySet_BodyReturned_ObjectResponse()
        {
            using (var sut = new ApiStub())
            {
                sut.Get("/testget", (req, args) => new { TestValue = 234, SubObject = new { Value = "Person" } });

                sut.Start();

                var result = await httpClient.GetAsync(new UriBuilder(new Uri(sut.Address)) { Path = "/testget" }.Uri);

                var resultString = await result.Content.ReadAsStringAsync();

                var resultObject = JsonConvert.DeserializeObject<JToken>(resultString);

                Assert.NotNull(resultObject);
                Assert.Equal(234, resultObject["TestValue"]);
                Assert.Equal("Person", resultObject["SubObject"]["Value"]);
            }
        }

        [Fact]
        public async Task StatusCode_StatusCodeSet_StatusCodeReturned()
        {
            using (var sut = new ApiStub())
            {
                sut.Get("/testget", (req, args) => "testresponse")
                    .StatusCode(StatusCodes.Status206PartialContent);

                sut.Start();

                var result = await httpClient.GetAsync(new UriBuilder(new Uri(sut.Address)) { Path = "/testget" }.Uri);

                Assert.Equal(HttpStatusCode.PartialContent, result.StatusCode);
            }
        }

        [Fact]
        public async Task StatusCode_StatusCodeProviderSet_StatusCodeReturned()
        {
            using (var sut = new ApiStub())
            {
                sut.Get("/testget", (req, args) => "testresponse")
                    .StatusCode((req, args) => args.Query.testquery == "Success" ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError);

                sut.Start();

                var resultSuccess = await httpClient.GetAsync(new UriBuilder(new Uri(sut.Address)) { Path = "/testget", Query = "?testquery=Success"}.Uri);

                Assert.Equal(HttpStatusCode.OK, resultSuccess.StatusCode);

                var resultFailure = await httpClient.GetAsync(new UriBuilder(new Uri(sut.Address)) { Path = "/testget", Query = "?testquery=Failure"}.Uri);

                Assert.Equal(HttpStatusCode.InternalServerError, resultFailure.StatusCode);
            }
        }

        [Fact]
        public async Task Header_HeadersAdded_HeadersReturned()
        {
            using (var sut = new ApiStub())
            {
                sut.Get("/testget", (req, args) => "testresponse")
                    .Header("Header1", "HeaderValue1")
                    .Header("Header2", "HeaderValue2");

                sut.Start();

                var result = await httpClient.GetAsync(new UriBuilder(new Uri(sut.Address)) { Path = "/testget" }.Uri);

                Assert.Equal("HeaderValue1", result.Headers.GetValues("Header1").First());
                Assert.Equal("HeaderValue2", result.Headers.GetValues("Header2").First());
            }
        }

        [Fact]
        public async Task Headers_HeadersAdded_HeadersReturned()
        {
            using (var sut = new ApiStub())
            {
                sut.Get("/testget", (req, args) => "testresponse")
                    .Headers(new KeyValuePair<string, string>("Header1", "HeaderValue1"), new KeyValuePair<string, string>("Header2", "HeaderValue2"));

                sut.Start();

                var result = await httpClient.GetAsync(new UriBuilder(new Uri(sut.Address)) { Path = "/testget" }.Uri);

                Assert.Equal("HeaderValue1", result.Headers.GetValues("Header1").First());
                Assert.Equal("HeaderValue2", result.Headers.GetValues("Header2").First());
            }
        }
    }
}