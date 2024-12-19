using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArmaBrowser.Data.DefaultImpl.Rest;
using ArmaBrowser.Properties;
using Newtonsoft.Json;

namespace ArmaBrowser.Logic
{
    internal static class AddonWebApi
    {
        //private const string BaseUrl = @"http://armabrowsertest.fakeland.de/";
#if DEBUG
        private const string ApiVer = "4";

        //private const string BaseUrl = @"http://homeserver/arma/api/" + ApiVer + @"/";
        private const string BaseUrl = @"http://armabrowser.org/api/" + ApiVer + @"/";
#else
        private const string ApiVer = "4";
        const string BaseUrl = @"http://armabrowser.org/api/" + ApiVer + @"/";
#endif

        private static HttpClient _client;
        private static TimeSpan _offset;
        private static string _ver;

        private static HttpClient RestClient
        {
            get
            {
                if (_client == null)
                {
                    var handler = new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    };

                    _client = new HttpClient(handler) {BaseAddress = new Uri(BaseUrl, UriKind.Absolute)};
#if DEBUG
                    _client.DefaultRequestHeaders.Add("XDEBUG_SESSION_START", "3B978CA5");
#endif
                    //_client.ClearHandlers();
                    //_client.AddHandler("application/json", new JsonDeserializer());

                    _client.DefaultRequestHeaders.Add("Accept-Language", Thread.CurrentThread.CurrentUICulture.Name);
                    _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
                    _client.DefaultRequestHeaders.Add("Accept", "application/json");
                    _client.DefaultRequestHeaders.Add("APPI", Settings.Default.Id);
                    var ver = GetAppVersion();
                    _client.DefaultRequestHeaders.Add("ClientVer", ver);
                }
                return _client;
            }
        }

        private static string GetAppVersion()
        {
            if (_ver != null) return _ver;

            _ver = Assembly.GetEntryAssembly().GetName().Version.ToString();
            return _ver;
        }


        private static async Task<HttpResponseMessage> ExecuteRequest(HttpRequestMessage request)
        {
            var restResult = await RestClient.SendAsync(request);

            if (restResult.StatusCode == HttpStatusCode.Unauthorized)
            {
                _offset = restResult.DateOffset();
                request = request.htua(_offset);

                restResult = await RestClient.SendAsync(request);
            }
            return restResult;
        }


        public static async Task PostInstalledAddonsKeysAsync(IEnumerable<IAddon> addons)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "Addons").htua();

            RestAddon[] restItems = addons.Where(a => a.IsInstalled).Select(a => new RestAddon
            {
                DisplayText = a.DisplayText,
                ModName = a.ModName,
                Name = a.Name,
                Version = a.Version,
                Keys = a.KeyNames.Select(k => new RestAddonKey {Key = k.Name, PubK = k.PubK.ToBase64()}).ToArray()
            }).ToArray();

            request.AddJsonBody(restItems);

            await ExecuteRequest(request);
        }

        public static async Task<RestAddonInfoResult[]> GetAddonInfosAsync(params string[] addonKeyNames)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "Addons/AddonInfo").htua(_offset);

                request.AddJsonBody(addonKeyNames);

                var restResult = await RestClient.SendAsync(request);

                if (restResult.StatusCode == HttpStatusCode.OK)
                {
                    var c = await restResult.Content.ReadAsStringAsync();
                    var o = JsonConvert.DeserializeObject<List<RestAddonInfoResult>>(c);
                    return o.ToArray();
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
            return new RestAddonInfoResult[0];
        }

        public static void AddAddonDownloadUri(IAddon addon, string uri)
        {
            throw new NotSupportedException();
            //try
            //{
            //    var key = addon.KeyNames.FirstOrDefault();
            //    RestAddonUri item = null;
            //    if (key != null)
            //    {
            //        item = new RestAddonUri
            //        {
            //            Hash = key.PubK.ToBase64().ComputeSha1Hash(),
            //            Uri = uri
            //        };
            //    }


            //    var request = new RestRequest("/Addons/AddAddonDownloadUri", Method.POST).htua(_offset);

            //    request.AddJsonBody(item);

            //    var restResult = RestClient.Execute(request);

            //    if (restResult.StatusCode == HttpStatusCode.OK)
            //    {
            //        return;
            //    }
            //    ;
            //}
            //catch (Exception exception)
            //{
            //    Debug.WriteLine(exception);
            //}
        }


        internal static void UploadAddon(IAddon addon)
        {
            //try
            //{
            //    var key = addon.KeyNames.FirstOrDefault();
            //    if (key != null)
            //    {
            //        var hash = key.PubK.ToBase64().ComputeSha1Hash();

            //        IRestRequest request = new RestRequest("/Addons/UploadAddon", Method.POST);


            //        var a = addon;
            //        const string zipFilePath = "tmp.zip";
            //        if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
            //        ZipFile.CreateFromDirectory(a.Path, zipFilePath, CompressionLevel.Optimal, true);
            //        try
            //        {
            //            using (var fs = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            //            {
            //                Action<Stream> d = delegate(Stream stream)
            //                {
            //                    var buffer = new byte[2048];

            //                    // ReSharper disable AccessToDisposedClosure
            //                    var readLen = fs.Read(buffer, 0, buffer.Length);
            //                    var totalTransmittedCount = 0L;
            //                    var aAitem = addon as Addon;
            //                    try
            //                    {
            //                        while (readLen > 0)
            //                        {
            //                            stream.Write(buffer, 0, readLen);
            //                            stream.Flush();
            //                            totalTransmittedCount += readLen;

            //                            if (aAitem != null)
            //                            {
            //                                var progress = Convert.ToInt32(totalTransmittedCount * 100L / fs.Length);
            //                                aAitem.ProgressValue = progress > 0 ? progress : 1;
            //                            }
            //                            if (totalTransmittedCount == fs.Length)
            //                                break;
            //                            readLen = fs.Read(buffer, 0, buffer.Length);
            //                        }

            //                    }
            //                    finally
            //                    {
            //                        if (aAitem != null)
            //                            aAitem.ProgressValue = 0;
            //                    }
            //                    // ReSharper restore AccessToDisposedClosure
            //                };


            //                var headerFileParam = new FileParameter
            //                {
            //                    ContentLength = fs.Length,
            //                    Name = hash,
            //                    FileName = addon.Name,
            //                    //ContentType = "application/zip",
            //                    //Writer = delegate(Stream stream) { fs.CopyTo(stream); }
            //                };

            //                headerFileParam.Writer = d;

            //                request.Files.Add(headerFileParam);

            //                //request.AlwaysMultipartFormData = false;

            //                request.AddParameter("hash", hash, ParameterType.GetOrPost);

            //                request.AddParameter("ACCEPT", "text/plain", ParameterType.HttpHeader);
            //                request = request.htua(_offset);
            //                var restResult = ExecuteRequest(request);

            //                if (restResult.StatusCode == HttpStatusCode.OK)
            //                {
            //                    addon.IsEasyInstallable = true;
            //                    // ReSharper disable once RedundantJumpStatement
            //                    return;
            //                }
            //            }
            //        }
            //        finally
            //        {
            //            File.Delete(zipFilePath);
            //        }
            //    }

            //}
            //catch (Exception exception)
            //{
            //    Debug.WriteLine(exception);
            //}
        }

        internal static void DownloadAddon(IAddon addon, string addonPubKeyHash, string targetFolder)
        {
            //    var request = new RestRequest("/addonfiles/" + addonPubKeyHash + ".zip", Method.GET);
            //    var tmpfile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            //    var md5Hash = "";

            //    //request.AddParameter("hash", addonPubKeyHash)
            //    //    .AddHeader("ACCEPT", "application/zip")
            //    //    .htua();

            //    request.ResponseWriter = (res, stream) =>
            //    {
            //        int fileLen = -1;
            //        var hLen = res.Headers.FirstOrDefault(h => "Content-Length".Equals(h.Name, StringComparison.OrdinalIgnoreCase));
            //        if (hLen != null)
            //            int.TryParse(hLen.Value, out fileLen);

            //        var md5Header = res.Headers.FirstOrDefault(h => "Content-MD5".Equals(h.Name, StringComparison.OrdinalIgnoreCase));
            //        if (md5Header != null)
            //            md5Hash = md5Header.Value;

            //        var s = stream;//as System.IO.Compression.GZipStream;
            //        var myAddon = addon as Addon;
            //        if (res.StatusCode == HttpStatusCode.OK
            //            //&& "application/zip".Equals(res.ContentType, StringComparison.OrdinalIgnoreCase)
            //            )
            //        {
            //            try
            //            {

            //                var buffer = new byte[1024 * 8];
            //                var downloadLen = 0;
            //                using (var tmpfs = new FileStream(tmpfile, FileMode.Open, FileAccess.Write))
            //                {
            //                    int len;
            //                    do
            //                    {
            //                        len = s.Read(buffer, 0, buffer.Length);
            //                        tmpfs.Write(buffer, 0, len);
            //                        downloadLen += len;
            //                        if (myAddon == null || downloadLen <= 0) continue;
            //                        var progress = Convert.ToInt32(downloadLen * 100L / fileLen);
            //                        myAddon.ProgressValue = progress > 0 ? progress : 1;
            //                    } while (len > 0);
            //                    tmpfs.Flush();
            //                    if (myAddon != null)
            //                        myAddon.ProgressValue = 0;

            //                }
            //            }
            //            finally
            //            {
            //                if (myAddon != null)
            //                    myAddon.ProgressValue = 0;
            //            }
            //        }
            //    };

            //    RestClient.ClearHandlers();
            //    ExecuteRequest(request);

            //    if (!string.IsNullOrEmpty(md5Hash))
            //    {
            //        using (var md5 = MD5.Create())
            //        {
            //            var calc = md5.FromFile(tmpfile);
            //            if (calc != md5Hash)
            //            {
            //                Trace.WriteLine("MD5 Check Fail");
            //            }
            //        }
            //    }

            //    if (File.Exists(tmpfile))
            //    {
            //        try
            //        {
            //            using (var tmpfs = new FileStream(tmpfile, FileMode.Open, FileAccess.Read))
            //            using (ZipArchive archive = new ZipArchive(tmpfs, ZipArchiveMode.Read, true))
            //            {
            //                archive.ExtractToDirectory(targetFolder);
            //            }

            //        }
            //        finally
            //        {
            //            File.Delete(tmpfile);
            //        }
            //    }
        }
    }


    internal static class RestRequestExtension
    {
        private const string A = @"ArmaServerBrowser";
        private const string V = @"2";
        private const string AhK = "appkey";

        #region public key

        private static readonly byte[] PubBlob =
        {
            0x6, 0x2, 0x0, 0x0, 0x0, 0xA4, 0x0, 0x0, 0x52, 0x53, 0x41, 0x31, 0x0, 0x10, 0x0, 0x0,
            0x1, 0x0, 0x1, 0x0, 0x85, 0x5B, 0xD6, 0x6E, 0x1B, 0x21, 0xA3, 0xEE, 0xC9, 0x62, 0x3B, 0xC2,
            0xD4, 0x14, 0x17, 0x61, 0xCA, 0xE1, 0x87, 0xE, 0xD3, 0xD7, 0xC1, 0x2F, 0x17, 0x9A, 0x34, 0x87,
            0xF7, 0x20, 0x70, 0x5A, 0xDB, 0x48, 0x4A, 0xFA, 0x56, 0xF5, 0x53, 0xF6, 0xE5, 0x46, 0xDF, 0x46,
            0x55, 0xD8, 0x20, 0x32, 0xB9, 0x21, 0x8B, 0x1D, 0x7B, 0xD2, 0xF7, 0xD1, 0x10, 0xE0, 0x7F, 0x86,
            0x1C, 0x9, 0xDB, 0xDB, 0xD5, 0x97, 0x80, 0x20, 0x43, 0xE5, 0x58, 0xCE, 0x78, 0xFA, 0x3A, 0x40,
            0x15, 0xB1, 0xB, 0x6A, 0xD, 0x7F, 0xDF, 0xFF, 0xC5, 0x0, 0x6D, 0x7B, 0x7C, 0x4, 0xA1, 0x43,
            0x64, 0xBA, 0xBF, 0x1E, 0x9E, 0x4E, 0x66, 0x43, 0x39, 0x6D, 0xF5, 0xA4, 0x17, 0x37, 0x3F, 0x0,
            0x62, 0x8D, 0xA6, 0x9, 0xC2, 0x7D, 0x1A, 0x21, 0xCC, 0x4B, 0xA3, 0xE7, 0x8, 0x99, 0xAE, 0x78,
            0x13, 0xE8, 0x7A, 0xF2, 0x7, 0x2B, 0xDC, 0xB3, 0x89, 0x6C, 0x79, 0x75, 0xF8, 0x1A, 0xD, 0x8A,
            0x7A, 0xEF, 0x78, 0x49, 0xF0, 0xB6, 0x1F, 0xC4, 0x64, 0xB9, 0x7B, 0xC1, 0xEC, 0x6B, 0xB6, 0x1F,
            0x62, 0xC0, 0x49, 0x3F, 0x83, 0xFB, 0xC1, 0x2F, 0xCE, 0x6E, 0xEF, 0xCF, 0xD4, 0xC4, 0x30, 0x42,
            0xBA, 0x51, 0xCE, 0x6F, 0x99, 0x45, 0x3A, 0x70, 0x2E, 0x1A, 0xAF, 0xB7, 0x3E, 0x43, 0x51, 0x47,
            0x62, 0xDA, 0x5E, 0x4A, 0x3, 0xEC, 0x30, 0xFB, 0x4B, 0x16, 0x57, 0xEF, 0x16, 0x14, 0xD1, 0x49,
            0xCD, 0xFD, 0x36, 0x3D, 0x6B, 0x6B, 0xC1, 0xF1, 0xD3, 0x11, 0xD6, 0x53, 0x3C, 0x1F, 0x92, 0x5C,
            0xB4, 0x4B, 0x71, 0x8D, 0x8D, 0x67, 0xC1, 0xE2, 0xC9, 0xDF, 0x63, 0x25, 0xDA, 0x2D, 0x9B, 0x26,
            0x77, 0x39, 0xF9, 0xFE, 0xB2, 0x9, 0x82, 0x6F, 0x32, 0xF0, 0x23, 0x81, 0x1D, 0x34, 0x61, 0xF9,
            0xD5, 0x5F, 0x1B, 0xF, 0x4A, 0xFC, 0x71, 0xB2, 0xFC, 0x3, 0xE1, 0x3C, 0x5C, 0x45, 0x3F, 0x7A,
            0xB, 0x89, 0x8F, 0x42, 0x36, 0xDC, 0x72, 0xE4, 0xA5, 0x28, 0x6, 0x34, 0x8C, 0x68, 0xA9, 0xC3,
            0x31, 0xF4, 0x28, 0xF1, 0xDE, 0xBF, 0xC6, 0xF6, 0x8E, 0x3F, 0x15, 0xF1, 0x1, 0x9D, 0x68, 0x78,
            0xCC, 0xE6, 0x48, 0x2B, 0xC7, 0x90, 0xE5, 0xD6, 0x2D, 0x89, 0xF6, 0x41, 0x8E, 0x7D, 0xFB, 0x8F,
            0x33, 0x6D, 0xC8, 0x14, 0x49, 0x21, 0xC9, 0xE0, 0x8B, 0xC8, 0xAB, 0x67, 0xE6, 0xBA, 0xAE, 0xA7,
            0xEC, 0x9E, 0x43, 0x61, 0x4A, 0xD7, 0x5, 0xCD, 0x45, 0xA6, 0x24, 0xEE, 0x74, 0xA, 0xB3, 0xBE,
            0xB6, 0x8D, 0x8, 0x23, 0x2B, 0x88, 0x65, 0xA2, 0xB1, 0x64, 0x32, 0xC7, 0x8C, 0xC1, 0x1E, 0x76,
            0x6, 0x99, 0x3F, 0xD7, 0x36, 0x9E, 0x3C, 0x74, 0x14, 0xC, 0x35, 0x50, 0x7B, 0x55, 0xCE, 0x7F,
            0xC4, 0xA7, 0xC4, 0x87, 0x6, 0x4F, 0x93, 0x7, 0xCA, 0x0, 0x1C, 0x3C, 0x8, 0x42, 0x1D, 0x92,
            0xDC, 0x63, 0x62, 0x8C, 0x82, 0x9B, 0xA3, 0x46, 0x6, 0xD8, 0x9B, 0x49, 0x5F, 0xFE, 0xE3, 0xD6,
            0x7E, 0x65, 0x7, 0xF6, 0xE6, 0xA3, 0x30, 0xF1, 0x5E, 0xD5, 0xA9, 0x5A, 0xE5, 0x7A, 0xC0, 0xB1,
            0x69, 0x3D, 0x57, 0x38, 0xB8, 0xD0, 0x69, 0x66, 0x92, 0x6C, 0xCA, 0x91, 0xB8, 0x22, 0x8C, 0x95,
            0xFF, 0x4B, 0xE3, 0xB, 0x9A, 0x53, 0x9C, 0x38, 0x40, 0x5D, 0xD2, 0x1A, 0xC0, 0xC4, 0xBC, 0xB,
            0x5C, 0x32, 0x2C, 0x4B, 0x36, 0xB8, 0x69, 0x99, 0xF9, 0xE9, 0x34, 0xB8, 0x7, 0xAB, 0xDF, 0x2E,
            0x9E, 0x69, 0x5, 0xE7, 0x3C, 0x74, 0xD5, 0x59, 0x10, 0xA3, 0xB1, 0x83, 0xDF, 0xEE, 0xF4, 0x60,
            0xBC, 0x89, 0xE0, 0x7E, 0xA8, 0xB, 0xEE, 0x6B, 0xF, 0x94, 0xCF, 0xB8, 0x1F, 0xFB, 0x17, 0x64,
            0x69, 0x4F, 0xA1, 0x9E
        };

        #endregion

        [SecurityCritical]
        // ReSharper disable once InconsistentNaming
        internal static HttpRequestMessage htua(this HttpRequestMessage request, TimeSpan offset = default(TimeSpan))
        {
            var time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour,
                DateTime.Now.Minute, 0).ToUniversalTime().Add(offset);

            string appkey;

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportCspBlob(PubBlob);
                //Debug.WriteLine(A + " \"" + time.ToUniversalTime().ToString("r") + "\" " + V + " " + Properties.Settings.Default.Id);
                appkey = Convert.ToBase64String(rsa.Encrypt(
                    Encoding.ASCII.GetBytes(A + " \"" + time.ToUniversalTime().ToString("r") + "\" " + V + " " +
                                            Settings.Default.Id), false));
            }

            request.Headers.Remove(AhK);
            request.Headers.Add(AhK, appkey);

            return request;
        }

        internal static TimeSpan DateOffset(this HttpResponseMessage response)
        {
            if (!response.Headers.Date.HasValue)
                return TimeSpan.Zero;

            var offset = response.Headers.Date.Value.DateTime - DateTime.UtcNow;
            return TimeSpan.FromMinutes(Math.Truncate(offset.TotalMinutes));
        }

        internal static string ComputeSha1Hash(this string s)
        {
            byte[] hashValue = Encoding.ASCII.GetBytes(s);
            return ComputeSha1Hash(hashValue);
        }

        internal static string ComputeSha1Hash(this byte[] b)
        {
            using (var hashAlg = HashAlgorithm.Create(@"SHA1"))
            {
                if (hashAlg == null) throw new NotImplementedException("SHA1");
                return hashAlg.ComputeHash(b).ToHexString();
            }
        }

        internal static void AddJsonBody(this HttpRequestMessage request, object content)
        {
            request.Content = new StringContent(JsonConvert.SerializeObject(content));
        }
    }
}