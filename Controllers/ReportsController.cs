using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    [Authorize(Roles = "Admin, Credit Officer, Analyst, Admin Assistant")]
    public class ReportsController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public ReportsController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
        {
            _userManager = userManager;
            _customSignInManager = customSignInManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            this._session = contextAccessor.HttpContext.Session;
        }

        public async Task<IActionResult> Adar()
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

        public async Task<IActionResult> PerAnalyst()
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

        // public IActionResult Tat()
        // {
        //     return View();
        // }
    }
}