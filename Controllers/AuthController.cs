using System;
using System.DirectoryServices;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using lrsms.Context;
using lrsms.Custom;
using lrsms.Dto;
using lrsms.Models;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Encrypt_JavaScript_Decrypt_Csharp;
namespace lrsms.Controllers
{
    public class AuthController : BaseController
    {
        private readonly DataContext _db;
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHttpContextAccessor _contextAccessor;
        private ISession _session;
        private readonly IDataProtector _dataProtector;
        public AuthController(DataContext context,DataContext db,IHttpContextAccessor contextAccessor, IMapper mapper, IDataProtectionProvider dataprovider ,CustomSignInManager<AppUser> customSignInManager,
            UserManager<AppUser> userManager) : base(context, mapper)
        {
            _db = db;
            _userManager = userManager;
            _customSignInManager = customSignInManager;
            this._session = contextAccessor.HttpContext.Session;
            this._dataProtector = dataprovider.CreateProtector("asasasa");
        }
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }
        [AllowAnonymous]
        public IActionResult ClearSession()
        {
            _session.SetString("logged","sdasda");
           return RedirectToAction("Index", "Home");
        }
        public ContentResult GetGroupId()
        {
        return Content(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "GroupId").ToString(), "application/json");
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult>Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return View(loginDto);
                
                var username = AESEncryption.DecryptStringAES(loginDto.Username);
                var password = AESEncryption.DecryptStringAES(loginDto.Password);
                var user = await _userManager.FindByNameAsync(username); 
                if (user != null )
                {
                    if(user.Session_Name == null)
                    {
                        string x = user.FullName + " " + DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss");
                        _session.SetString("sessionname",x);
                        user.Session_Name = _session.GetString("sessionname");
                        _session.SetString("username",user.UserName);
                       _session.SetString("id",user.Id.ToString());
                        _context.SaveChanges();
                        var userRole = await _userManager.GetRolesAsync(user);
                        await _customSignInManager.SignInAsync(user, userRole);
                        return RedirectToAction("Index", "Home");
                    }
                    
                    else if(user.Session_Name != null && user.Session_Name != _session.GetString("sessionname") )
                    {
                        _session.SetString("logged","loggedin");
                        _session.SetString("uname", user.UserName);
                        return RedirectToAction("Login", "Auth");
                    }
                    else 
                    {
                        ViewBag.WrongCreds = "sadasdsa";
                        return View (loginDto);
                    }
                    
                } 
                else if (user == null)
                {
                    ViewBag.WrongCreds = "Incorrect Windows ID or Password";
                    return View(loginDto);
                }
                else if (_customSignInManager.IsUserLockedOut(user.UserName))
                {
                    ViewBag.WrongCreds = "Your account is temporarily blocked. Please try again after 30 minutes.";
                    return View(loginDto);
                }
                else if (!await _customSignInManager.ValidateWindowsAuth(username, password))
                {
                    ViewBag.WrongCreds = "Incorrect Windows ID or Password";
                    return View(loginDto);
                }
                else if (!user.IsActive)
                {
                    ViewBag.WrongCreds = "The account of this user has not been activated yet.";
                    return View(loginDto);
                }
                else 
                {
                    ViewBag.WrongCreds = "Please Login Again";
                    return View (loginDto);
                }
            }
            [AllowAnonymous]
            public async Task<IActionResult> SecondLogin()
            {
                var username = _session.GetString("uname");
                var user = await _userManager.FindByNameAsync(username); 
                if (user != null )
                {
                    string x = user.FullName + " " + DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss");
                    _session.SetString("sessionname",x);
                    user.Session_Name = _session.GetString("sessionname");
                    _session.SetString("username",user.UserName);
                    _session.SetString("id",user.Id.ToString());
                    _context.SaveChanges();
                    var userRole = await _userManager.GetRolesAsync(user);
                    await _customSignInManager.SignInAsync(user, userRole);
                    _session.SetString("asdddd",user.UserName);
                    return RedirectToAction("Index", "Home");
                }
                else 
                {
                    _session.SetString("asdddd","dsadasd");
                    return RedirectToAction("Login", "Auth");
                }
                
            }
            [HttpPost]
            public async Task<IActionResult> Logout()
            {  
                var userId = _userManager.GetUserId(User);
                var users = await _userManager.FindByNameAsync(User.Identity.Name);
                users.Session_Name = null;
                _context.SaveChanges();
                _session.Clear();
                await _customSignInManager.SignOutAsync();
                    return RedirectToAction("Login", "Auth");
            }
            
            public ContentResult ValidateLdapUsername(string ldapname)
            {
                DirectoryEntry directoryEntry = new DirectoryEntry("LDAP://172.20.185.15");
                DirectorySearcher directorySearcher = new DirectorySearcher(directoryEntry);
                directorySearcher.Filter = $"(SAMAccountName={ldapname})";
                LdapFullNameToReturn ldapFullName = new LdapFullNameToReturn();
                try
                {
                    SearchResult result = directorySearcher.FindOne();
                    if (result == null)
                    {
                        directoryEntry.Close();
                    }
                    else
                    {
                        directorySearcher.PropertiesToLoad.Add("givenName");
                        directorySearcher.PropertiesToLoad.Add("sn");
                        string givenName = (string)result.Properties["givenName"][0];
                        string surName = (string)result.Properties["sn"][0];
                        ldapFullName.FullName = $"{givenName} {surName}".Trim();
                        directoryEntry.Close();
                    }
                }
                catch
                {
                    Response.StatusCode = 400;
                    return Content("Bad Request");
                }
                return Content(JsonConvert.SerializeObject(ldapFullName), "application/json");
            }
        }
}