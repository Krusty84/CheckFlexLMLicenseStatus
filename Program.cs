﻿/*
Program.cs
16.01.2022 1:35:02
Alexey Sedoykin
*/

namespace CheckFlexLMLicenseStatus
{
    using Newtonsoft.Json;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Web.Http;
    using System.Web.Http.SelfHost;
    using System.Xml.Linq;
    using Topshelf;

    /// <summary>
    /// Defines the <see cref="LicenseStatusController" />.
    /// </summary>
    [RoutePrefix("api/licensestatus")]
    public class LicenseStatusController : ApiController
    {
        //http://127.0.0.1:5555/api/licensestatus/CheckLicenseExpired
        /* plus this body:
                "{\n   \"licVendorName\":\"ugslmd\",\n   \"licServerPort\":\"28000\",\n   \"licServerName\":\"localhost\",\n
        \"licFeatureToCheck\":\"teamcenter_author\",\n   \"expireDateRaw\":15,\n
        \"lmutilPath\":\"C:\\\\Siemens\\\\PLMLicenseServer\\\\lmutil.exe\"\n}"
        */

        [HttpGet]
        public object CheckLicenseExpired([FromBody] string payload)
        {
            CheckLicenseExpiredPayload response = JsonConvert.DeserializeObject<CheckLicenseExpiredPayload>(payload);
            Console.WriteLine("Lic Vendor Name: " + response.licVendorName);
            Console.WriteLine("Lic Server Port: " + response.licServerPort);
            Console.WriteLine("Lic Lmutil Path: " + response.lmutilPath);
            Console.WriteLine("Lic Server Name: " + response.licServerName);
            Console.WriteLine("Lic Feature Check: " + response.licFeatureToCheck);
            Console.WriteLine("Lic Expire Date: " + response.dangeousDaysBeforeExpire.ToString());
            //
            var licStatusResp = new LicenseStatusResponce();
            string expireDateRaw;
            try
            {
                //проверяем запущен ли сервер лицензии
                Process[] pname = Process.GetProcessesByName(response.licVendorName);
                if (pname.Length == 0)
                {
                    licStatusResp.licenseStatus = "FlexLM, Vendor is missing or didn't start";
                    return Json(licStatusResp);
                }
                else
                {
                    string args = String.Format("lmstat -i -c {0} -f {1}", (response.licServerPort + "@" + response.licServerName), response.licFeatureToCheck);
                    ProcessStartInfo pcStartInfo = new ProcessStartInfo(response.lmutilPath, args);
                    pcStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    pcStartInfo.UseShellExecute = false;
                    pcStartInfo.RedirectStandardOutput = true;
                    using (Process prcLMUtil = Process.Start(pcStartInfo))
                    {
                        string output = prcLMUtil.StandardOutput.ReadToEnd();

                        //максимальное время ожидания, 5 мин.
                        if (prcLMUtil.WaitForExit(300000))
                        {
                            prcLMUtil.WaitForExit();
                        }
                        else
                        {
                            prcLMUtil.Kill();
                            prcLMUtil.WaitForExit();
                        }
                        File.WriteAllText("C:\\Siemens\\PLMLicenseServer\\checkedLicense.log", output);
                    }
                    //разрбор лог-файла, взятие из предпоследней строки - даты истечения лицензии
                    var LinesFromLmutilLog = File.ReadAllLines(@"C:\\Siemens\\PLMLicenseServer\\checkedLicense.log");
                    var LastStringOfLog = LinesFromLmutilLog[LinesFromLmutilLog.Length - 1];
                    expireDateRaw = LastStringOfLog.ToString().Substring(LastStringOfLog.ToString().Length - 11);
                    DateTime expiredDate = DateTime.Parse(expireDateRaw);
                    DateTime currentDate = DateTime.Now;
                    Console.WriteLine("Current Date: " + currentDate.ToString("dd-MM-yyyy"));
                    Console.WriteLine("Expired Date: " + expiredDate.ToString());
                    TimeSpan valueOfWorkinTime = ((TimeSpan)(expiredDate - DateTime.Now));
                    Console.WriteLine("Different date: " + valueOfWorkinTime.TotalDays.ToString());
                    if (valueOfWorkinTime.TotalDays < 0)
                    {
                        Console.WriteLine("Expired! " + expiredDate.ToString());
                        licStatusResp.expiredDate = expiredDate.ToString();
                        licStatusResp.licenseStatus = "Expired!";
                        return Json(licStatusResp);
                    }
                    else if (valueOfWorkinTime.TotalDays <= response.dangeousDaysBeforeExpire)
                    {
                        Console.WriteLine("License will expire in less than " + response.dangeousDaysBeforeExpire + " days!" + expiredDate.ToString());
                        licStatusResp.expiredDate = expiredDate.ToString();
                        licStatusResp.licenseStatus = "License will expire in less than " + response.dangeousDaysBeforeExpire + " days!" + expiredDate.ToString();
                        return Json(licStatusResp);
                    }
                    else if (valueOfWorkinTime.TotalDays > 0)
                    {
                        Console.WriteLine("Ok! End of: " + expiredDate.ToString());
                        licStatusResp.expiredDate = expiredDate.ToString();
                        licStatusResp.licenseStatus = "Ok! End of: " + expiredDate.ToString();
                        return Json(licStatusResp);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something was wrong: " + ex.ToString());
            }
            return payload;
        }
    }

    /// <summary>
    /// Defines the <see cref="SomeThingController" />.
    /// </summary>
    [RoutePrefix("api/something")]
    public class SomeThingController : ApiController
    {
        //http://127.0.0.1:5555/api/something/sum?a=10&b=10
        /// <summary>
        /// The Sum.
        /// </summary>
        /// <param name="a">The a<see cref="int"/>.</param>
        /// <param name="b">The b<see cref="int"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        [HttpGet]
        public string Sum(int a, int b)
        {
            return "Sum:" + (a + b).ToString();
        }

        /// <summary>
        /// The Hello.
        /// </summary>
        /// <returns>The <see cref="object"/>.</returns>
        [HttpGet]
        public object Hello()
        {
            var licStatusResp = new LicenseStatusResponce();
            licStatusResp.licenseStatus = "GooOd";
            licStatusResp.expiredDate = "Yeasterday";
            return Json(licStatusResp);
        }

        /// <summary>
        /// The Bay.
        /// </summary>
        /// <returns>The <see cref="object"/>.</returns>
        [HttpGet]
        public object Bay()
        {
            return "Bay-bay page";
        }
    }

    /// <summary>
    /// Defines the <see cref="CheckFlexLMLicenseStatusService" />.
    /// </summary>
    internal class CheckFlexLMLicenseStatusService
    {
        /// <summary>
        /// Defines the server.
        /// </summary>
        private readonly HttpSelfHostServer server;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckFlexLMLicenseStatusService"/> class.
        /// </summary>
        public CheckFlexLMLicenseStatusService(string listeningPort)
        {
            var selfHostConfiguraiton = new HttpSelfHostConfiguration("http://127.0.0.1:" + listeningPort);
            selfHostConfiguraiton.Routes.MapHttpRoute(
                name: "DefaultApiRoute",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
                );

            server = new HttpSelfHostServer(selfHostConfiguraiton);
        }

        /// <summary>
        /// The Start.
        /// </summary>
        public void Start()
        {
            server.OpenAsync();
        }

        /// <summary>
        /// The Stop.
        /// </summary>
        public void Stop()
        {
            server.CloseAsync();
            server.Dispose();
        }
    }

    /// <summary>
    /// Defines the <see cref="Program" />.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The Main.
        /// </summary>
        /// <param name="args">The args<see cref="string[]"/>.</param>
        private static void Main(string[] args)
        {

            XDocument xmlConfigFile = XDocument.Load("configuration.xml");
            string httpListeningPort = xmlConfigFile.Descendants("httpListeningPort").First().Value;
            HostFactory.Run(x =>
                {
                    x.Service<CheckFlexLMLicenseStatusService>(s =>
                    {
                        s.ConstructUsing(name => new CheckFlexLMLicenseStatusService(httpListeningPort));
                        s.WhenStarted(svc => svc.Start());
                        s.WhenStopped(svc => svc.Stop());
                    });

                    x.RunAsLocalSystem();
                    x.SetDescription("CheckFlexLMLicenseStatus");
                    x.SetDisplayName("CheckFlexLMLicenseStatus");
                    x.SetServiceName("CheckFlexLMLicenseStatus");
                });
        }
    }
}