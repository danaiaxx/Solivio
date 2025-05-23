using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Solivio.Data;
using Solivio.Models;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Solivio.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Feed(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u =>
                (u.Email == email || u.Username == email) && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("Username", user.Username);
                return View("Feed");
            }

            ViewBag.Error = "Invalid email/username or password.";
            return View("Login");
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.LoggedInUsername = HttpContext.Session.GetString("Username");
            base.OnActionExecuting(context);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signup(string email, string password, string username, string securityQuestion, string securityAnswer)
        {
            if (_context.Users.Any(u => u.Email == email || u.Username == username))
            {
                ViewBag.Error = "Email or Username already exists.";
                return View();
            }

            var user = new User
            {
                Email = email,
                Password = password,
                Username = username,
                SecurityQuestion = securityQuestion,
                SecurityAnswer = securityAnswer,
                ProfileImageData = new byte[0]
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        public IActionResult Forgotpass()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Securityquestion(string emailOrUsername, string securityAnswer)
        {
            var input = emailOrUsername?.Trim();

            var user = _context.Users.FirstOrDefault(u =>
                (u.Email == input || u.Username == input));

            if (user == null)
            {
                ViewBag.Error = "Email or Username not found.";
                return View("Forgotpass");
            }

            if (user.SecurityAnswer != securityAnswer)
            {
                ViewBag.Error = "Incorrect security answer.";
                ViewData["EmailOrUsername"] = input;
                ViewData["SecurityQuestion"] = user.SecurityQuestion;
                return View();
            }

            return RedirectToAction("Resetpass", new { emailOrUsername = input });
        }

        [HttpGet]
        public IActionResult Resetpass(string emailOrUsername)
        {
            if (string.IsNullOrEmpty(emailOrUsername))
            {
                return RedirectToAction("Forgotpass");
            }

            ViewData["EmailOrUsername"] = emailOrUsername;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Resetpass(string emailOrUsername, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Please enter both password fields.";
                ViewData["EmailOrUsername"] = emailOrUsername;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                ViewData["EmailOrUsername"] = emailOrUsername;
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == emailOrUsername || u.Username == emailOrUsername);
            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return View("Forgotpass");
            }

            user.Password = newPassword;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        public IActionResult Feed()
        {
            return View();
        }


        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Settings()
        {
            string? username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return RedirectToAction("Login");

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Settings(
            string? username,
            string? email,
            string? currentPassword,
            string? newPassword,
            string? confirmPassword,
            string? formType,
            IFormFile? profileImage)
        {
            string? sessionUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(sessionUsername))
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Username == sessionUsername);
            if (user == null)
                return RedirectToAction("Login");

            if (formType == "account")
            {
                user.Username = username ?? user.Username;
                user.Email = email ?? user.Email;

                if (profileImage != null && profileImage.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
                    Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        await profileImage.CopyToAsync(memoryStream);
                        user.ProfileImageData = memoryStream.ToArray();
                        user.ProfileImageContentType = profileImage.ContentType;
                    }

                    user.ProfileImage = "/images/profiles/" + uniqueFileName;
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                HttpContext.Session.SetString("Username", user.Username);

                TempData["AccountSuccess"] = "Personal details updated.";
            }
            else if (formType == "password")
            {
                if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                {
                    TempData["PasswordError"] = "Please fill in all password fields.";
                }
                else if (user.Password != currentPassword)
                {
                    TempData["PasswordError"] = "Current password is incorrect.";
                }
                else if (newPassword != confirmPassword)
                {
                    TempData["PasswordError"] = "New password and confirmation do not match.";
                }
                else
                {
                    user.Password = newPassword;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();

                    TempData["PasswordSuccess"] = "Password updated.";
                }
            }
            ViewBag.ProfileImageUrl = Url.Action("GetProfileImage", "Home") + "?t=" + DateTime.Now.Ticks;

            return View(user);
        }

        public IActionResult GetProfileImage()
        {
            string? username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return File("~/images/default-profile.png", "image/png");

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return File("~/images/default-profile.png", "image/png");

            if (user.ProfileImageData != null && user.ProfileImageData.Length > 0)
            {
                string contentType = user.ProfileImageContentType ?? "image/png";
                return File(user.ProfileImageData, contentType);
            }

            if (!string.IsNullOrEmpty(user.ProfileImage))
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfileImage.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    var contentType = "image/" + Path.GetExtension(fullPath).Trim('.');
                    return PhysicalFile(fullPath, contentType);
                }
            }

            return File("~/images/default-profile.png", "image/png");
        }


        public IActionResult Createpost()
        {
            return View();
        }

        public IActionResult Post(int id)
        {
            ViewBag.PostId = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(List<IFormFile> media, string caption, string location)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            if (media == null || media.Count == 0)
            {
                ModelState.AddModelError("media", "Please upload at least one image.");
                return View();
            }
            if (media.Count > 5)
            {
                ModelState.AddModelError("media", "You can upload up to 5 images only.");
                return View();
            }
            if (string.IsNullOrWhiteSpace(caption) || string.IsNullOrWhiteSpace(location))
            {
                ModelState.AddModelError(string.Empty, "Caption and Location are required.");
                return View();
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Get logged-in user id from claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized();
            }

            var post = new Post
            {
                Caption = caption,
                Location = location,
                UserId = currentUserId,
                DatePosted = DateTime.UtcNow,
                Images = new List<PostImage>()
            };

            foreach (var file in media)
            {
                if (file.Length > 0 && file.ContentType.StartsWith("image/"))
                {
                    var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        var fileBytes = ms.ToArray();

                        post.Images.Add(new PostImage
                        {
                            ImageData = fileBytes,
                            ContentType = file.ContentType
                        });
                    }
                }
                else
                {
                    ModelState.AddModelError("media", "Only image files are allowed.");
                    return View();
                }
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Post created successfully!";
            return RedirectToAction("CreatePost");
        }
    }
}

