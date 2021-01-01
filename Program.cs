using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using System.Xml.Linq;
using Topshelf;


namespace CheckFlexLMLicenseStatus
{

    [RoutePrefix("api/licensestatus")]
    public class LicenseStatusController : ApiController
    {
        //http://127.0.0.1:5555/api/licensestatus/checkexistflexlm?check=true&flexdaemon=ugslmd
        [HttpGet]
        public string CheckExistFLEXLM(bool check, string flexdaemon)
        {
            if (check == true)
            {
                Process[] pname = Process.GetProcessesByName(flexdaemon);
                if (pname.Length == 0)
                    return "flexlm missing";
                else
                    return "flexlm exist";
            }

            return null;
        }

        [HttpGet]
        public string CheckLicenseExpired()
        {
            string lmutilPath;
            string licServerPort;
            string licServerName;
            string licFeatureToCheck;
            string expireDateRaw;
            int dangeousDaysBeforeExpire;
            //
            try
            {
                XDocument xmlConfigFile = XDocument.Load("configuration.xml");
                lmutilPath = xmlConfigFile.Descendants("lmutilPath").First().Value;
                licServerPort = xmlConfigFile.Descendants("licServerPort").First().Value;
                licServerName = xmlConfigFile.Descendants("licServerName").First().Value;
                licFeatureToCheck = xmlConfigFile.Descendants("checkingFeature").First().Value;
                dangeousDaysBeforeExpire = Int16.Parse(xmlConfigFile.Descendants("dangeousDaysBeforeExpire").First().Value);
                //
                string args = String.Format("lmstat -i -c {0} -f {1}", (licServerPort+"@"+licServerName), licFeatureToCheck);
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
                //
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
                    return "License was expired! End of: " + expiredDate.ToString();
                }

                else if (valueOfWorkinTime.TotalDays <= dangeousDaysBeforeExpire)
                {
                    Console.WriteLine("License will expire in less than "+dangeousDaysBeforeExpire+" days!" + expiredDate.ToString());
                    return "License will expire in less than two weeks! End of: " + expiredDate.ToString();
                }
                else if (valueOfWorkinTime.TotalDays > 0)
                {
                    Console.WriteLine("License is okay! End of: " + expiredDate.ToString());
                    return "License is okay! End of: " + expiredDate.ToString();
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("Something was wrong: " + ex.ToString());
            }

            return null;
        }

    }


    [RoutePrefix("api/something")]
    public class SomeThingController : ApiController
    {
        //http://127.0.0.1:5555/api/something/sum?a=10&b=10
        [HttpGet]
        public string Sum(int a, int b)
        {
            return "Sum:" + (a+b).ToString();
        }

        [HttpGet]
        public object Hello()
        {
            return "Hello page";
        }

        [HttpGet]
        public object Bay()
        {
            return "Bay-bay page";
        }
    }


    class CheckFlexLMLicenseStatusService
    {
        private readonly HttpSelfHostServer server;

        public CheckFlexLMLicenseStatusService()
        {


            var selfHostConfiguraiton = new HttpSelfHostConfiguration("http://127.0.0.1:"+ "5555");

            selfHostConfiguraiton.Routes.MapHttpRoute(
                name: "DefaultApiRoute",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
                );

            server = new HttpSelfHostServer(selfHostConfiguraiton);
        }

        public void Start()
        {
            server.OpenAsync();
        }

        public void Stop()
        {
            server.CloseAsync();
            server.Dispose();
        }

    }
    internal static class Program
    {
        static void Main(string[] args)
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
