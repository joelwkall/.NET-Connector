using System;
using System.Collections.Generic;
using Loop54.Exceptions;
using Loop54.Model;
using NUnit.Framework;

namespace Loop54.Tests
{
    [TestFixture]
    public class CallMethods
    {
        private Request CreateRequest(string method)
        {
            var request = new Request(method, null, new RequestOptions { MeasureTime=true});
            request.UserId = "testUser";
            request.IP = "0.0.0.0";

            return request;
        }

        private Response GetResponse(Request request)
        {
            return RequestHandling.GetResponse("http://helloworld.54proxy.com", request);
        }

        [TestCase("testuser","0.0.0.0","beef","Search",ExpectedResult = null)]
        [TestCase(null, "0.0.0.0", "beef", "Search", ExpectedResult = null)]
        [TestCase(null, null, "beef", "Search", ExpectedResult = typeof(ArgumentNullException))]
        [TestCase("testuser", null, "beef", "Search", ExpectedResult = typeof(ArgumentNullException))]
        [TestCase("testuser", "0.0.0.0", null, "Search", ExpectedResult = typeof(ArgumentNullException))]
        [TestCase("testuser", "0.0.0.0", "beef", null, ExpectedResult = typeof(EngineErrorException))]
        public Type InvalidRequests(string userId,string IP,string query, string method)
        {
            try
            {
                var request = new Request(method, null, new RequestOptions { MeasureTime = true });
                request.UserId = userId;
                request.IP = IP;
                request.SetValue("QueryString",query);

                GetResponse(request);
            }
            catch (Exception ex)
            {
                return ex.GetType();
            }

            return null;
        }

        [Test]
        public void CreateEvents()
        {
            var request = CreateRequest("CreateEvents");
            var events = new Event[] { new Event() { Type = "click", Entity = new Entity("123", "321") } };
            request.SetValue("Events", events);

            var response = GetResponse(request);
        }


        [Test]
        public void AutoCompleteHasResults([Values("b","be","bee","beef","c","ch","chi","chic","chick","chicke","chicken")]string query)
        {
            var request = CreateRequest("AutoComplete");
            request.SetValue("QueryString", query);

            var response = GetResponse(request);

            var results = response.GetValue<List<Entity>>("AutoComplete");

            Assert.Greater(results.Count,0);
        }

        [Test]
        public void SearchHasResults([Values("steak", "chicken breast")]string query, [Values("DirectResults", "RecommendedResults")]string resultsType)
        {
            var request = CreateRequest("Search");
            request.Options.MeasureTime = true;
            request.SetValue("QueryString", query);

            var response = GetResponse(request);

            Assert.Greater(response.GetValue<double>(resultsType + "_TotalItems"), 0);
            Assert.Less(response.ResponseTime, 100);
        }

        [Test]
        public void SearchLimits([Values("steak", "chicken breast")]string query,[Values(2,5,10,100)]int number, [Values("DirectResults","RecommendedResults")]string resultsType)
        {
            var request = CreateRequest("Search");
            request.SetValue("QueryString", query);
            request.SetValue(resultsType+"_FromIndex",0);
            request.SetValue(resultsType + "_ToIndex", number-1);

            var response = GetResponse(request);

            var numDirect = response.GetValue<double>(resultsType + "_TotalItems");
            var results = response.GetValue<List<Entity>>(resultsType);

            Assert.LessOrEqual(results.Count, numDirect, "The engine returned more results than the engine reported existed."); //do not return more than exist
            Assert.LessOrEqual(results.Count, number, "The engine returned more results than we asked for."); //do not return more than we asked for

            //if there are more than we asked for
            if(numDirect>number)
                Assert.AreEqual(results.Count, number); //return exactly as many as we asked for
            else
                Assert.AreEqual(results.Count, numDirect); //return exactly as many as exist
        }
    }
}