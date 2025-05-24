using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Solivio.Data;
using Solivio.Models;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
                HttpContext.Session.SetInt32("UserId", user.Id);
                return RedirectToAction("Feed");
            }

            ViewBag.Error = "Invalid email/username or password.";
            return View("Login");
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.LoggedInUsername = HttpContext.Session.GetString("Username");
            base.OnActionExecuting(context);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
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

        public async Task<IActionResult> Feed()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                var allPosts = await _context.Posts
                    .AsNoTracking()
                    .Include(p => p.User)
                    .Include(p => p.Images)
                    .OrderByDescending(p => p.DatePosted)
                    .ToListAsync();

                int userPostCount = await _context.Posts
                .Where(p => p.UserId == userId)
                .CountAsync();

                ViewBag.PostCount = userPostCount;

                foreach (var post in allPosts)
                {
                    if (post.Images == null)
                    {
                        post.Images = new List<PostImage>();
                    }

                    Console.WriteLine($"Post {post.Id}: User={post.User?.Username}, Images={post.Images.Count}, Caption={post.Caption}");
                }

                return View(allPosts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading posts: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return View(new List<Post>());
            }
        }


        public async Task<IActionResult> Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var userPosts = await _context.Posts
                .Where(p => p.UserId == userId)
                .Include(p => p.Images)
                .OrderByDescending(p => p.DatePosted)
                .ToListAsync();

            ViewBag.PostCount = userPosts.Count;

            return View(userPosts);
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

        public IActionResult GetUserProfileImage(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(List<IFormFile> media, string caption, string location)
        {
            string? username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (media == null || media.Count == 0)
            {
                ModelState.AddModelError("media", "Please upload at least one image.");
                return View("Createpost");
            }

            if (media.Count > 5)
            {
                ModelState.AddModelError("media", "You can upload up to 5 images only.");
                return View("Createpost");
            }

            if (string.IsNullOrWhiteSpace(caption))
            {
                ModelState.AddModelError("caption", "Caption is required.");
                return View("Createpost");
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                ModelState.AddModelError("location", "Location is required.");
                return View("Createpost");
            }

            try
            {
                var post = new Post
                {
                    Caption = caption.Trim(),
                    Location = location.Trim(),
                    UserId = user.Id,
                    DatePosted = DateTime.UtcNow,
                    Images = new List<PostImage>()
                };

                foreach (var file in media)
                {
                    if (file.Length > 0 && file.ContentType.StartsWith("image/"))
                    {
                        if (file.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("media", $"Image {file.FileName} is too large. Maximum size is 5MB.");
                            return View("Createpost");
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            await file.CopyToAsync(memoryStream);
                            var imageData = memoryStream.ToArray();

                            var postImage = new PostImage
                            {
                                ImageData = imageData,
                                ContentType = file.ContentType,
                                Post = post
                            };

                            post.Images.Add(postImage);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("media", "Only image files are allowed.");
                        return View("Createpost");
                    }
                }

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Post created successfully!";
                return RedirectToAction("Createpost");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post for user {Username}", username);

                ModelState.AddModelError("", "An error occurred while creating your post. Please try again.");
                return View("Createpost");
            }
        }

        public IActionResult GetPostImage(int imageId)
        {
            var postImage = _context.Set<PostImage>().FirstOrDefault(pi => pi.Id == imageId);

            if (postImage?.ImageData != null)
            {
                string contentType = postImage.ContentType ?? "image/jpeg";
                return File(postImage.ImageData, contentType);
            }

            return NotFound();
        }
    }
}

