/*
Program.cs
16.01.2022 1:35:02
Alexey Sedoykin
*/

namespace CheckFlexLMLicenseStatus
{
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
        /// <summary>
        /// The CheckLicenseExpired.
        /// </summary>
        /// <returns>The <see cref="object"/>.</returns>
        [HttpGet]
        public object CheckLicenseExpired()
        {
            string lmutilPath;
            string licServerPort;
            string licServerName;
            string licFeatureToCheck;
            string expireDateRaw;
            string licVendorName;
            int dangeousDaysBeforeExpire;
            //
            var licStatusResp = new LicenseStatusResponce();
            //
            try
            {
                XDocument xmlConfigFile = XDocument.Load("configuration.xml");
                lmutilPath = xmlConfigFile.Descendants("lmutilPath").First().Value;
                licServerPort = xmlConfigFile.Descendants("licServerPort").First().Value;
                licServerName = xmlConfigFile.Descendants("licServerName").First().Value;
                licFeatureToCheck = xmlConfigFile.Descendants("checkingFeature").First().Value;
                licVendorName = xmlConfigFile.Descendants("licVendorName").First().Value;
                dangeousDaysBeforeExpire = Int16.Parse(xmlConfigFile.Descendants("dangeousDaysBeforeExpire").First().Value);
                //проверяем запущен ли сервер лицензии
                Process[] pname = Process.GetProcessesByName(licVendorName);
                if (pname.Length == 0)
                {
                    licStatusResp.licenseStatus = "FlexLM, Vendor is missing or didn't start";
                    return Json(licStatusResp);
                }
                else
                {
                    string args = String.Format("lmstat -i -c {0} -f {1}", (licServerPort + "@" + licServerName), licFeatureToCheck);
                    ProcessStartInfo pcStartInfo = new ProcessStartInfo(lmutilPath, args);
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
                        Console.WriteLine("License was expired! " + expiredDate.ToString());
                        licStatusResp.expiredDate = expiredDate.ToString();
                        licStatusResp.licenseStatus = "License was expired";
                        return Json(licStatusResp);
                    }
                    else if (valueOfWorkinTime.TotalDays <= dangeousDaysBeforeExpire)
                    {
                        Console.WriteLine("License will expire in less than " + dangeousDaysBeforeExpire + " days!" + expiredDate.ToString());
                        licStatusResp.expiredDate = expiredDate.ToString();
                        licStatusResp.licenseStatus = "License will expire in less than two weeks!";
                        return Json(licStatusResp);
                    }
                    else if (valueOfWorkinTime.TotalDays > 0)
                    {
                        Console.WriteLine("License is okay! End of: " + expiredDate.ToString());
                        licStatusResp.expiredDate = expiredDate.ToString();
                        licStatusResp.licenseStatus = "License is okay!";
                        return Json(licStatusResp);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something was wrong: " + ex.ToString());
            }
            return null;
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
        public CheckFlexLMLicenseStatusService()
        {
            var selfHostConfiguraiton = new HttpSelfHostConfiguration("http://127.0.0.1:" + "5555");
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
            HostFactory.Run(x =>
                {
                    x.Service<CheckFlexLMLicenseStatusService>(s =>
                    {
                        s.ConstructUsing(name => new CheckFlexLMLicenseStatusService());
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
