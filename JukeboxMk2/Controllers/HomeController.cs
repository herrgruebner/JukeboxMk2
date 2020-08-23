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
        public IActionResult GetAuthorisationCode()
        {
            //todo user submitted playlist ids or smaller codes. Guids are cool, but not user friendly
            var name = Guid.NewGuid();
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
                JukeBoxId = state,
            };
            var db = new Db();
            db.InsertData(data);

            data.PlaylistId = spotify.CreateNewPlaylist(data); // sets the playlist id in userdata

            db.UpdatePlaylistId(data);
            ViewBag.Message = $"Success, playlist created";
            return View("Search", new SearchModel() { PlaylistId = data.JukeBoxId, Songs = new List<Song>() });
        }

        public IActionResult Search(string name, string playlistId)
        {
            var spotify = new Spotify();
            var tracks = spotify.SearchSongs(name, playlistId);
            if (tracks == null)
            {
                ViewBag.ErrorMessage = "tracks is null";
                return View("Error");
            }
            return View(new SearchModel() { Songs = tracks, PlaylistId = playlistId});
        }

        public IActionResult AddSong(string id, string title, string playlistId)
        {
            var data = new Db().GetData().FirstOrDefault(s => s.JukeBoxId == playlistId);
            var spotify = new Spotify();
            spotify.AddSong(id, data);
            ViewBag.Message = $"Success, '{title}' added";
            return View("Search", new SearchModel() {Songs = new List<Song>(), PlaylistId = playlistId});
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
