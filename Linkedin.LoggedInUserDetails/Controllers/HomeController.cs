using Linkedin.LoggedInUserDetails.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace Linkedin.LoggedInUserDetails.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        

        public ActionResult RedirectLinkedIN(string code, string state)
        {
            try
            {
                // Get User Token
                var client = new RestClient("https://www.linkedin.com");
                var request = new RestRequest("/oauth/v2/accessToken",Method.Post);
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                request.AddParameter("grant_type", "authorization_code");
                request.AddParameter("code", Request.Query["code"].ToString());
                request.AddParameter("redirect_uri", "https://localhost:7015/Home/RedirectLinkedIN");
                request.AddParameter("client_id", "77nw4xwvs6cvcf");
                request.AddParameter("client_secret", "PQ0HeLti9gwnJuhX");
                
                var response = client.Execute(request);
                var content = response.Content;
                var res = (JObject)JsonConvert.DeserializeObject(content);
                
                // Get User Details
                var client2 = new RestClient("https://api.linkedin.com");
                var request2 = new RestRequest("/v2/userinfo",Method.Get);
                request2.AddHeader("Authorization", $"Bearer {Convert.ToString(res["access_token"])}");

                var response2 = client2.Execute(request2);
                var content2 = response2.Content;
                var res2 = (JObject)JsonConvert.DeserializeObject(content2);

                var userInfo = new LinkedINUserInfo() {
                    Name = Convert.ToString(res2["name"]),
                    Image = Convert.ToString(res2["picture"])
                };
                return View(userInfo);
            }
            catch (Exception)
            {
                View("Index");
            }
            return View();
        }

        public async Task<ActionResult> OverlayedInfo(string name,string path)
        {
            string outputPath = OverlayImageAndTextWithBackground(await SaveImageFromURL(path), name);
            return View(new LinkedINUserInfo() { 
                Image = outputPath.Substring(outputPath.IndexOf("wwwroot", 0) + ("wwwroot").Length),
                Name = name
            });
        }



        private string OverlayImageAndTextWithBackground(string path,string text)
        {
            using Bitmap image = new Bitmap(path);

            EmbedNameOnImage(image, text);
            OverlayImage(image);

            return SaveEditedImage(image);
        }
        private void OverlayImage(Bitmap image)
        {

            using (Graphics graphics = Graphics.FromImage(image))
            {
                // Create a semi-transparent red brush
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, Color.Red)))
                {
                    // Fill a rectangle the size of the image with the red brush
                    graphics.FillRectangle(brush, 0, 0, image.Width, image.Height);
                }
            }
        }
        private void EmbedNameOnImage(Bitmap image,string text)
        {
            using (Graphics graphics = Graphics.FromImage(image))
            {
                // Use anti-aliasing for smoother text rendering
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

                // Draw the text on the image
                graphics.DrawString(text, new Font("Arial", 10), Brushes.Black, new Point(0, 50));
            }
            //image.Save(@"D:\output2.jpg", ImageFormat.Jpeg);
        }
        private string SaveEditedImage(Bitmap image)
        {

            string wwwrootPath = _webHostEnvironment.WebRootPath;
            string imagePath = Path.Combine(wwwrootPath, "linkedin", "output.png");

            image.Save(imagePath, ImageFormat.Jpeg);
            return imagePath;
        }

        public async Task<string> SaveImageFromURL(string imageUrl, string subfolder = "linkedin")
        {
            try
            {
                using HttpClient client = new HttpClient();
                byte[] imageData = await client.GetByteArrayAsync(imageUrl);

                string wwwrootPath = _webHostEnvironment.WebRootPath;
                string imagePath = Path.Combine(wwwrootPath, subfolder, GetFileNameFromUrl(imageUrl));

                string savedPath = string.Concat(imagePath, ".png");

                await System.IO.File.WriteAllBytesAsync(savedPath, imageData);
                return savedPath;

            }
            catch (Exception ex)
            {
            }
            return "";
        }
        private string GetFileNameFromUrl(string url)
        {
            Uri uri = new Uri(url);
            return Path.GetFileName(uri.LocalPath);
        }
    }
 
}