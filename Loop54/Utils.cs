﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;
using Loop54.Exceptions;

namespace Loop54.Utils
{
    internal static class Strings
    {
        internal static string FixEngineUrl(string url)
        {
            var ret = url.ToLower().Trim().Replace("\\","/");

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                throw new Exception("Invalid url: Url must use protocol http.");

            if (!ret.EndsWith("/"))
                ret += "/";

            return ret;
        }

        internal static string Escape(string val)
        {
            return val.Replace("\"", "\\\"").Replace("\t", "").Replace("\n", "").Replace("\r", "");
        }

        internal static string Random(int length, bool lowerCase)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < length; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }
    }

    internal static class Http
    {
        internal struct HttpResponse
        {
            public string Content;
            public long ContentLength;

            public long RoundtripTime;
            public long RequestTime;
            public long EngineTime;
            public long ResponseTime;
            public long ReadDataTime;

            public long SetUpSPMTime; //2.0.8
            public long CreateRequestTime; //2.0.8
            public long AddHeadersTime; //2.0.8
        }

        internal static string GetUser()
        {
            var context = HttpContext.Current;

            if (context == null)
                return null;

            //get existing cooke
            var cookie = context.Request.Cookies["Loop54User"];

            if (cookie != null)
                return cookie.Value;

            //create new cookie
            var userId = GetIP().Replace(":",".") + "_" + Strings.Random(10, false);

            var newCookie = new HttpCookie("Loop54User");
            newCookie.Expires = DateTime.Now.AddYears(1);
            newCookie.Value = userId;

            context.Response.Cookies.Add(newCookie);

            return userId;
        }

        internal static string GetReferer()
        {
            var context = HttpContext.Current;

            if (context == null)
                return null;

            return context.Request.ServerVariables["HTTP_REFERER"];
        }

        internal static string GetUserAgent()
        {
            var context = HttpContext.Current;

            if (context == null)
                return null;

            return context.Request.ServerVariables["HTTP_USER_AGENT"];
        }

        internal static string GetUrl()
        {
            var context = HttpContext.Current;

            if (context == null)
                return null;

            return context.Request.Url.AbsoluteUri;
        }

        internal static string GetIP()
        {
            var context = HttpContext.Current;

            if (context == null)
                return null;

            //behind proxy?
            var forwarded = context.Request.Headers["X-Forwarded-For"];

            if (!string.IsNullOrEmpty(forwarded))
                return forwarded;

            return context.Request.ServerVariables["REMOTE_ADDR"];
        }

        
        private static int _consecutiveFailures = 0;
        private static DateTime _fallbackDate = DateTime.MinValue;

        private static bool IsOnFallBack(int numFailures, int minutesOnFallBack)
        {
            
            //has already fallen back before
            if (_fallbackDate > DateTime.UtcNow.AddMinutes(-minutesOnFallBack))
            {
                _consecutiveFailures = 0; //reset this when fallen back
                return true;
            }

            //first fallback request
            if (_consecutiveFailures >= numFailures)
            {
                _fallbackDate = DateTime.UtcNow;
                _consecutiveFailures = 0; //reset this when fallen back

                return true;
            }

            //no fallback
            _fallbackDate = DateTime.MinValue;
            return false;

        }

        internal static HttpResponse GetEngineResponse(string url, string verb, string data, int timeout,bool measureTime, int numFailuresToFallBack,int minutesOnFallBack)
        {
            Stopwatch watch=null;
            if (measureTime)
            {
                watch=new Stopwatch();
                watch.Start();
            }


            //on fallback? switch to 108proxy
            if (IsOnFallBack(numFailuresToFallBack, minutesOnFallBack))
            {
                url = url.ToLower(); //for safetys sake

                if (url.Contains(".54proxy."))
                {
                    url = url.Replace(".54proxy.", ".108proxy.");
                }
            }

            HttpResponse httpResponse;
            try
            {
                httpResponse = GetResponseData2(url, verb, data, timeout,measureTime:measureTime);

                //reset failures on each successful request
                _consecutiveFailures = 0;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;

                if (ex.Message.StartsWith("The remote name could not be resolved:"))
                    throw new EngineNotFoundException(url, ex);
                if (ex.Message.StartsWith("The operation has timed out"))
                    throw new EngineNotFoundException(url, ex);
                if (ex.Message.StartsWith("The remote server returned an error"))
                    throw new EngineErrorException(url, ex);

                throw;
            }

            if (measureTime)
            {
                watch.Stop();

                httpResponse.RoundtripTime = watch.ElapsedMilliseconds;
            }

            return httpResponse;
        }

        //private static bool _setUpSPM = false;
        //private static void SetUpSPM()
        //{
        //    if (_setUpSPM)
        //        return;

        //    ServicePointManager.UseNagleAlgorithm = false;

        //    ServicePointManager.Expect100Continue = false; //2.0.8

        //    ServicePointManager.DefaultConnectionLimit = 48; //2.0.8 (var 24 innan)

        //    _setUpSPM = true;
        //}

        private static HttpResponse GetResponseData2(string url, string verb = "GET", string stringData = null,
                                              int timeout = 5000, Encoding dataEncoding = null,
                                              IDictionary<string, string> headers = null, bool measureTime=false)
        {

            var ret = new HttpResponse();

            if (dataEncoding == null)
                dataEncoding = Encoding.UTF8;


            Stopwatch watch = null;
            if (measureTime)
            {
                watch = new Stopwatch();
                watch.Start();
            }

            //borttaget i 2.0.14
            //SetUpSPM(); //2.0.8

            //if (measureTime)
            //{
            //    watch.Stop();
            //    ret.SetUpSPMTime = watch.ElapsedMilliseconds;
            //    watch.Reset();
            //    watch.Start();
            //}


            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = verb;
            request.Timeout = timeout;
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Proxy = null;  //2.0.8
            request.SendChunked = false; //2.0.9
            request.KeepAlive = false; //2.0.12

            request.ServicePoint.UseNagleAlgorithm = false; //2.0.12
            request.ServicePoint.Expect100Continue = false; //2.0.12
            request.ServicePoint.ConnectionLimit = 5000; //2.0.18
            request.ServicePoint.MaxIdleTime = 120000; //2.0.14
   

            if (measureTime)
            {
                watch.Stop();
                ret.CreateRequestTime = watch.ElapsedMilliseconds;
                watch.Reset();
                watch.Start();
            }

            if(headers!=null)
                foreach (var headerPair in headers)
                    request.Headers[headerPair.Key] = headerPair.Value;

            if (measureTime)
            {
                watch.Stop();
                ret.AddHeadersTime = watch.ElapsedMilliseconds;
                watch.Reset();
                watch.Start();
            }

           
            if (stringData != null)
            {
                var buffer = dataEncoding.GetBytes(stringData);

                //modifierat i 2.0.9
                //using i 2.0.12
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
            }

            
            if (measureTime)
            {
                watch.Stop();
                ret.RequestTime = watch.ElapsedMilliseconds;

                watch.Reset();
                watch.Start();
            }

            //lagt till using i 2.0.12
            using (var response = (HttpWebResponse) request.GetResponse())
            {


                if (measureTime)
                {
                    watch.Stop();
                    ret.ResponseTime = watch.ElapsedMilliseconds;

                    long.TryParse(response.GetResponseHeader("Time"), out ret.EngineTime);

                    watch.Reset();
                    watch.Start();
                }

                //modifierat i 2.0.9
                //lagt till using i 2.0.12
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var reader = new StreamReader(responseStream, dataEncoding))
                        {
                            ret.Content = reader.ReadToEnd();
                        }
                    }
                }

                //tillagt i 2.0.19
                if (measureTime)
                {
                    watch.Stop();
                    ret.ReadDataTime = watch.ElapsedMilliseconds;

                    watch.Reset();

                }

                long.TryParse(response.GetResponseHeader("Content-Length"), out ret.ContentLength);

            }

            

            return ret;
        }

      
    }

    
}