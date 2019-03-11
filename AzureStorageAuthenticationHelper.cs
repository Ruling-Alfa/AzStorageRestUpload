using System;
using System.Collections;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace StorageRestApiAuth
{
    /// <summary>
    /// You can take this class and drop it into another project and use this code
    /// to create the headers you need to make a REST API call to Azure Storage.
    /// </summary>
    internal static class AzureStorageAuthenticationHelper
    {
        /// <summary>
        /// This creates the authorization header. This is required, and must be built 
        ///   exactly following the instructions. This will return the authorization header
        ///   for most storage service calls.
        /// Create a string of the message signature and then encrypt it.
        /// </summary>
        /// <param name="storageAccountName">The name of the storage account to use.</param>
        /// <param name="storageAccountKey">The access key for the storage account to be used.</param>
        /// <param name="now">Date/Time stamp for now.</param>
        /// <param name="httpRequestMessage">The HttpWebRequest that needs an auth header.</param>
        /// <param name="ifMatch">Provide an eTag, and it will only make changes
        /// to a blob if the current eTag matches, to ensure you don't overwrite someone else's changes.</param>
        /// <param name="md5">Provide the md5 and it will check and make sure it matches the blob's md5.
        /// If it doesn't match, it won't return a value.</param>
        /// <returns></returns>

        internal static string AuthorizationHeader(string storageAccountName, string storageAccountKey,
            string method, DateTime now, HttpWebRequest request, string ifMatch = "", string md5 = "")
        {
            string MessageSignature;

            //this is the raw representation of the message signature 
            MessageSignature = String.Format("{0}\n\n\n{1}\n{5}\n\n\n\n{2}\n\n\n\n{3}{4}",
                method,
                (method == "GET" || method == "HEAD") ? String.Empty : request.ContentLength.ToString(),
                ifMatch,
                GetCanonicalizedHeaders(request),
                GetCanonicalizedResource(request.RequestUri, storageAccountName),
                md5
                );

            //now turn it into a byte array
            byte[] SignatureBytes = System.Text.Encoding.UTF8.GetBytes(MessageSignature);

            //create the HMACSHA256 version of the storage key
            System.Security.Cryptography.HMACSHA256 SHA256 =
                new System.Security.Cryptography.HMACSHA256(Convert.FromBase64String(storageAccountKey));

            //Compute the hash of the SignatureBytes and convert it to a base64 string.
            string signature = Convert.ToBase64String(SHA256.ComputeHash(SignatureBytes));

            //this is the actual header that will be added to the list of request headers
            string AuthorizationHeader = "SharedKey " + storageAccountName
                + ":" + Convert.ToBase64String(SHA256.ComputeHash(SignatureBytes));

            return AuthorizationHeader;
        }

        /// <summary>
        /// Put the headers that start with x-ms in a list and sort them.
        /// Then format them into a string of [key:value\n] values concatenated into one string.
        /// (Canonicalized Headers = headers where the format is standardized).
        /// </summary>
        /// <param name="httpRequestMessage">The request that will be made to the storage service.</param>
        /// <returns>Error message; blank if okay.</returns>

        public static string GetCanonicalizedHeaders(HttpWebRequest request)
        {
            ArrayList headerNameList = new ArrayList();
            StringBuilder sb = new StringBuilder();

            //retrieve any headers starting with x-ms-, put them in a list and sort them by value.
            foreach (string headerName in request.Headers.Keys)
            {
                if (headerName.ToLowerInvariant().StartsWith("x-ms-", StringComparison.Ordinal))
                {
                    headerNameList.Add(headerName.ToLowerInvariant());
                }
            }
            headerNameList.Sort();

            //create the string that will be the in the right format
            foreach (string headerName in headerNameList)
            {
                StringBuilder builder = new StringBuilder(headerName);
                string separator = ":";
                //get the value for each header, strip out \r\n if found, append it with the key
                foreach (string headerValue in GetHeaderValues(request.Headers, headerName))
                {
                    string trimmedValue = headerValue.Replace("\r\n", String.Empty);
                    builder.Append(separator);
                    builder.Append(trimmedValue);
                    //set this to a comma; this will only be used 
                    //if there are multiple values for one of the headers
                    separator = ",";
                }
                sb.Append(builder.ToString());
                sb.Append("\n");
            }
            return sb.ToString();
        }

        public static ArrayList GetHeaderValues(NameValueCollection headers, string headerName)
        {
            ArrayList list = new ArrayList();
            string[] values = headers.GetValues(headerName);
            if (values != null)
            {
                foreach (string str in values)
                {
                    list.Add(str.TrimStart(null));
                }
            }
            return list;
        }

        /// <summary>
        /// This part of the signature string represents the storage account 
        ///   targeted by the request. Will also include any additional query parameters/values.
        /// For ListContainers, this will return something like this:
        ///   /storageaccountname/\ncomp:list
        /// </summary>
        /// <param name="address">The URI of the storage service.</param>
        /// <param name="accountName">The storage account name.</param>
        /// <returns>String representing the canonicalized resource.</returns>
        private static string GetCanonicalizedResource(Uri address, string accountName)
        {
            StringBuilder str = new StringBuilder();
            StringBuilder builder = new StringBuilder("/");
            builder.Append(accountName);     //this is testsnapshots
            builder.Append(address.AbsolutePath);  //this is "/" because for getting a list of containers 
            str.Append(builder.ToString());

            NameValueCollection values2 = new NameValueCollection();
            //address.Query is ?comp=list
            //this ends up with a namevaluecollection with 1 entry having key=comp, value=list 
            //it will have more entries if you have more query parameters
            NameValueCollection values = System.Web.HttpUtility.ParseQueryString(address.Query);
            foreach (string str2 in values.Keys)
            {
                ArrayList list = new ArrayList(values.GetValues(str2));
                list.Sort();
                StringBuilder builder2 = new StringBuilder();
                foreach (object obj2 in list)
                {
                    if (builder2.Length > 0)
                    {
                        builder2.Append(",");
                    }
                    builder2.Append(obj2.ToString());
                }
                values2.Add((str2 == null) ? str2 : str2.ToLowerInvariant(), builder2.ToString());
            }
            ArrayList list2 = new ArrayList(values2.AllKeys);
            list2.Sort();
            foreach (string str3 in list2)
            {
                StringBuilder builder3 = new StringBuilder(string.Empty);
                builder3.Append(str3);
                builder3.Append(":");
                builder3.Append(values2[str3]);
                str.Append("\n");
                str.Append(builder3.ToString());
            }
            return str.ToString();
        }
    }
}
