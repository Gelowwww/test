using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using lrsms.Context;
using lrsms.Custom;
using lrsms.Dto;
using lrsms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

namespace lrsms.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public UserController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
        {
           _userManager = userManager;
            _customSignInManager = customSignInManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            this._session = contextAccessor.HttpContext.Session;
        }

        public async Task<IActionResult> Index()
        {
            if(_session.GetString("id")  != null && _session.GetString("sessionname") != null)
            {
                var id = Int32.Parse( _session.GetString("id"));
                var sessionname = _session.GetString("sessionname");
                var user = await _context.Users.ProjectTo<UserForDetailedAndEditDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id == id);
                var x = user.Session_Name;
                if (x.ToString() == sessionname)
                {
                    return View();
                }
                else
                {
                    _session.Clear();
                    _customSignInManager.SignOutAsync();
                    return RedirectToAction("Login", "Auth");
                }
            }
            else 
            {
                 _session.Clear();
                _customSignInManager.SignOutAsync();
                return RedirectToAction("Login", "Auth");
            }
            
            
        }

        public async Task<IActionResult> ViewUserDetails(int id)
        {
            var user = await _context.Users.ProjectTo<UserForDetailedAndEditDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id == id);

            return View(user);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.RoleList = RoleList().Result;
            ViewBag.GroupList = GroupList().Result;

            var user = await _context.Users.ProjectTo<UserForDetailedAndEditDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id == id);

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UserForDetailedAndEditDto userForDetailedAndEditDto)
        {
            ViewBag.RoleList = RoleList().Result;
            ViewBag.GroupList = GroupList().Result;

            if(!ModelState.IsValid)
                return View(userForDetailedAndEditDto);

            var user = await _userManager.FindByIdAsync(userForDetailedAndEditDto.Id.ToString());

            user.IsActive = userForDetailedAndEditDto.IsActive;
            user.Session = userForDetailedAndEditDto.Session;

            await _context.SaveChangesAsync();
            
            var oldUserRole = await _userManager.GetRolesAsync(user);

            await _userManager.RemoveFromRolesAsync(user ,oldUserRole);

            var newRole = await _roleManager.FindByIdAsync(userForDetailedAndEditDto.Role.ToString());
            await _userManager.AddToRoleAsync(user, newRole.Name);


            var userGroup = await _context.GroupUsers.SingleOrDefaultAsync(x => x.AppUserId == userForDetailedAndEditDto.Id);

            if(userGroup != null)
                _context.GroupUsers.Remove(userGroup);

            await _context.GroupUsers.AddAsync(new GroupUser()
            {
                AppUserId = userForDetailedAndEditDto.Id, GroupId = Convert.ToInt32(userForDetailedAndEditDto.Group)
            });

            await _context.SaveChangesAsync();
            
            return RedirectToAction("Index", "User");

        }

        public IActionResult Register()
        {
            ViewBag.RoleList = RoleList().Result;
            ViewBag.GroupList = GroupList().Result;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserForAddDto userForAddDto)
        {
             ViewBag.RoleList = RoleList().Result;
             ViewBag.GroupList = GroupList().Result;
            if(ModelState.IsValid)
            {
                AppUser userToAdd = new AppUser()
                {
                    UserName = userForAddDto.UserName,
                    FullName = userForAddDto.FullName,
                    IsActive = userForAddDto.IsActive
                };

                var result = await _userManager.CreateAsync(userToAdd);
                if(result.Succeeded)
                {
                    var role = _roleManager.FindByIdAsync(userForAddDto.Role);
                    await _userManager.AddToRoleAsync(userToAdd, role.Result.Name);

                    await _context.SaveChangesAsync();

                    var user = await _userManager.FindByNameAsync(userForAddDto.UserName);

                    var userId = user.Id;

                    await _context.GroupUsers.AddAsync(new GroupUser()
                    {
                        GroupId = Convert.ToInt32(userForAddDto.Group),
                        AppUserId = userId,
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                } 
            }
            
            return View(userForAddDto);
        }

        public async Task<List<SelectListItem>> RoleList()
        {
            var roles = await _context.Roles.ToListAsync();

            List<SelectListItem> list = new List<SelectListItem>();

            list.Add(new SelectListItem{ Text = "", Value = ""});

            foreach(var role in roles)
            {
                list.Add(new SelectListItem{Text = role.Name, Value = role.Id.ToString()});
            }

            return list;
        }

        public async Task<List<SelectListItem>> GroupList()
        {
            var groups = await _context.Groups.AsNoTracking().ToListAsync();

            List<SelectListItem> groupList = new List<SelectListItem>();

            groupList.Add(new SelectListItem{ Text = "", Value = ""});

            foreach (var group in groups)
            {
                groupList.Add(new SelectListItem{Text = group.GroupName, Value = group.Id.ToString()});
            }

            return groupList;
        }
    }
}