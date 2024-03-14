using BoroHFR.ViewModels.Setup;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using MailKit.Net.Smtp;
using System.Net;
using System.Security.Cryptography;

namespace BoroHFR.Controllers
{
    public class SetupController : Controller
    {
        private readonly IConfiguration _configuration;
        public SetupController( IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("dbdata")]
        public IActionResult DbData()
        {
            if (_configuration["ConnectionStrings:Default"] is not null)
            {
                return RedirectToAction("AuthData");
            }
            return View(new DbDataViewModel());
        }

        [HttpPost("dbdata")]
        public async Task<IActionResult> DbData(DbDataViewModel data)
        {
            if (!ModelState.IsValid)
            {
                return View(data);
            }
            MySqlConnectionStringBuilder builder = new();
            builder.Server = data.Server;
            builder.Port = data.Port;
            builder.UserID = data.User;
            builder.Password = data.Password;
            builder.Database = data.Database;
            string connStr = builder.ToString();

            using MySqlConnection conn = new(connStr);
            try
            {
                await conn.OpenAsync();
                _configuration["ConnectionStrings:Default"] = connStr;
            }
            catch
            {
                data.FailedToConnect = true;
                return View(data);
            }

            return RedirectToAction("AuthData");
            
        }

        [HttpGet("authdata")]
        public IActionResult AuthData()
        {
            if (_configuration["SysAdmin"] is not null)
            {
                return RedirectToAction("Smtp");
            }
            return View();
        }

        [HttpPost("authdata")]
        public IActionResult AuthData(AuthDataViewModel data)
        {
            if (!ModelState.IsValid)
            {
                return View(data);
            }

            var pwhash = BCrypt.Net.BCrypt.EnhancedHashPassword(data.Password);
            _configuration["SysAdmin:Email"] = data.Email;
            _configuration["SysAdmin:PasswordHash"] = pwhash;

            return RedirectToAction("Smtp");
        }

        [HttpGet("smtp")]
        public IActionResult Smtp()
        {
            if (_configuration["Smtp"] is not null)
            {
                return RedirectToAction("Done");
            }
            return View();
        }

        [HttpPost("smtp")]
        public async  Task<IActionResult> Smtp(SmtpViewModel data)
        {
            if (!ModelState.IsValid)
            {
                return View(data);
            }

            var serverEndPoint = new DnsEndPoint(data.Server, (int)data.Port);
            var credential = new NetworkCredential(data.Username, data.Password);
            using SmtpClient client = new();
            try
            {
                await client.ConnectAsync(serverEndPoint.Host, serverEndPoint.Port);
                await client.AuthenticateAsync(credential);
                await client.DisconnectAsync(true);
            }
            catch (Exception)
            {
                data.ConnectionFailed = true;
                return View(data);
            }


            var section = _configuration.GetSection("Smtp");
            section["Server"] = data.Server;
            section["Port"] = data.Port.ToString();
            section["Username"] = data.Username;
            section["Password"] = data.Password;
            section["SenderAddress"] = data.SenderAddress;
            section["SenderName"] = data.SenderName;

            return RedirectToAction("Done");
        }

        [HttpGet("done")]
        public IActionResult Done()
        {
            return View();
        }

        
    }
}
