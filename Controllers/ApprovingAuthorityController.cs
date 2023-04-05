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
    [Authorize(Roles = "Admin, Credit Officer")]
    [Authorize(Policy = "HoOnly")]
    public class ApprovingAuthorityController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public ApprovingAuthorityController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
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

        public async Task<IActionResult> ViewApprovingAuthorityDetails(int id)
        {
            var selectedApprovingAuthority = await _context.ApprovingAuthorities.ProjectTo<AuthorityForDetailedDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(x => x.Id == id);

            if (selectedApprovingAuthority == null)
                return NotFound();

            return View(selectedApprovingAuthority);
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(ApprovingAuthority approvingAuthority)
        {
            if (ModelState.IsValid)
            {
                var findApprovingAuthority = await _context.ApprovingAuthorities
                    .AnyAsync(x => x.ApprovingAuthorityName.ToLower() == approvingAuthority.ApprovingAuthorityName.ToLower());

                if (findApprovingAuthority)
                {
                    ViewBag.Exist = "This approving authority already exists!";

                    return View(approvingAuthority);
                }

                _context.Entry(approvingAuthority).State = EntityState.Added;

                approvingAuthority.CreatedAt = DateTime.Now;
                approvingAuthority.CreatedBy = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(approvingAuthority);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var selectedApprovingAuthority = await _context.ApprovingAuthorities.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedApprovingAuthority == null)
                return NotFound();

            return View(selectedApprovingAuthority);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ApprovingAuthority approvingAuthority)
        {
            if (ModelState.IsValid)
            {
                var approvingAuthorityList = await _context.ApprovingAuthorities.Where(x => x.Id != approvingAuthority.Id).ToListAsync();
                var findApprovingAuthority = approvingAuthorityList
                    .Any(x => x.ApprovingAuthorityName.ToLower() == approvingAuthority.ApprovingAuthorityName.ToLower());

                if (findApprovingAuthority)
                {
                    ViewBag.Exist = "This approving authority already exists!";

                    return View(approvingAuthority);
                }

                var appr = _context.Entry(approvingAuthority);
                appr.State = EntityState.Modified;
                appr.Property(x => x.CreatedAt).IsModified = false;
                appr.Property(x => x.CreatedBy).IsModified = false;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(approvingAuthority);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var selectedApprovingAuthority = await _context.ApprovingAuthorities.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedApprovingAuthority != null)
            {
                _context.Remove(selectedApprovingAuthority);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}