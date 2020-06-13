using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using JukeboxMk2.Models;

namespace JukeboxMk2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult GetAuthorisationCode(string name)
        {
            var scopes = "playlist-read-collaborative%20playlist-modify-public";
            var clientid = Environment.GetEnvironmentVariable("ClientId");
            return Redirect($"https://accounts.spotify.com/authorize?client_id={clientid}&response_type=code&redirect_uri={GenerateRedirectUri()}&scope={scopes}&state={name}");
        }

        public IActionResult SpotifySignin(string code, string state, string error)
        {
            if (error != string.Empty)
            {

            }
            var spotify = new Spotify();
            var tokens = spotify.GenerateAccessRefreshTokens(code, GenerateRedirectUri());
            var data = new UserData()
            {
                AccessToken = tokens.access_token,
                RefreshToken = tokens.refresh_token,
                Name = state
            };
            var db = new Db();
            db.InsertData(data);
            return View("Index");
        }
        public IActionResult Search(string name)
        {
            var spotify = new Spotify();
            var tracks = spotify.SearchSongs(name);
            if (tracks == null)
            {
                ViewBag.ErrorMessage = "tracks is null";
                return View("Error");
            }
            return View(tracks);
        }
        public IActionResult AddSong(string id, string title)
        {
            var spotify = new Spotify();
            spotify.AddSong(id);
            ViewBag.Message = $"Success, '{title}' added";
            return View("Index");
        }
        public IActionResult AccountList()
        {
            var data = new Db().GetData();

            return View(data);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        private string GenerateRedirectUri()
        {
            var uri = "https://" + HttpContext.Request.Host.Value + "/home/SpotifySignin";

            return uri;
        }
    }
}
