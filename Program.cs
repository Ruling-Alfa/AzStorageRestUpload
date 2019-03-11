namespace StorageRestApiAuth
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;

    internal static class Program
    {
        static string StorageAccountName = "";
        static string StorageAccountKey = "";

        public static void Main()
        {
            UploadFileREST(StorageAccountName, "entityphotos", StorageAccountKey,"Hello World", "test.txt");
            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }
        private static string UploadFileREST(string storageAccountName, string containerName, string storageAccountKey, string fileData, string fileName)
        {
            string result = null;
            HttpWebRequest request = CreateRESTRequest(StorageAccountName, storageAccountKey, containerName, fileName, "PUT", fileData);
            try
            {
                var response = request.GetResponse() as HttpWebResponse;

                //if successfully created (status code 201), parse the response, 
                //  which is null :P
                if ((int)response.StatusCode == 201)
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        result = reader.ReadToEnd();
                    }
                }
                response.Close();
            }
            catch (Exception ex)
            {

            }
            return result;
        }

        private static HttpWebRequest CreateRESTRequest(string storageAccountName, string storageAccountKey,
                    string containerName, string fileName,
                    string method, string requestBody = null,
                    SortedList<string, string> headers = null, string ifMatch = "", string md5 = "")
        {
            byte[] byteArray = null;
            DateTime now = DateTime.UtcNow;
            string uri = string.Format("https://{0}.blob.core.windows.net/{1}/{2}", storageAccountName, containerName, fileName);

            HttpWebRequest request = HttpWebRequest.Create(uri) as HttpWebRequest;
            request.Method = method;
            request.ContentLength = 0;

            request.Headers.Add("x-ms-date", now.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            request.Headers.Add("x-ms-version", "2014-02-14");
            request.Headers.Add("x-ms-blob-type", "BlockBlob");
            //if there are additional headers required, they will be passed in to here,
            //add them to the list of request headers
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
            //if there is a requestBody, add a header for the Accept-Charset and set the content length
            if (!String.IsNullOrEmpty(requestBody))
            {
                request.Headers.Add("Accept-Charset", "UTF-8");

                byteArray = System.Text.Encoding.UTF8.GetBytes(requestBody);
                request.ContentLength = byteArray.Length;
            }

            request.Headers.Add("Authorization", AzureStorageAuthenticationHelper.AuthorizationHeader(storageAccountName, storageAccountKey, method, now, request, ifMatch, md5));

            //now set the body in the request object 
            if (!String.IsNullOrEmpty(requestBody))
            {
                request.GetRequestStream().Write(byteArray, 0, byteArray.Length);
            }
            return request;
        }
    }
}
