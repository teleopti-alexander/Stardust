﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stardust.Manager.Interfaces;

namespace Stardust.Manager
{
    public class HttpSender : IHttpSender
    {
        public async Task<HttpResponseMessage> PostAsync(string url,
                                                         object data)
        {
            using (var client = new HttpClient())
            {
                string sez = JsonConvert.SerializeObject(data);

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    var response =
                        await client.PostAsync(url,
                                               new StringContent(sez,
                                                                 Encoding.Unicode,
                                                                 "application/json"));
                    return response;
                }
                catch (HttpRequestException)
                {
                    return null;
                }
            }
        }

        public async Task<HttpResponseMessage> DeleteAsync(string url,
                                                           Guid jobId)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    var response =
                        await client.DeleteAsync(url + "/" + jobId);

                    return response;
                }

                catch (HttpRequestException)
                {
                    return null;
                }
            }
        }
    }
}