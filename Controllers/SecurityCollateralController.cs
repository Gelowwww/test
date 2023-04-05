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
    public class SecurityCollateralController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public SecurityCollateralController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
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

        public async Task<IActionResult> ViewSecurityCollateralDetails(int id)
        {
            var selectedSecurityCollateral = await _context.SecurityCollaterals.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedSecurityCollateral == null)
                return NotFound();

            return View(selectedSecurityCollateral);
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(SecurityCollateral securityCollateral)
        {
            if (ModelState.IsValid)
            {
                var findSecurityCollateral = await _context.SecurityCollaterals
                    .AnyAsync(x => x.SecurityCollateralName.ToLower() == securityCollateral.SecurityCollateralName.ToLower());

                if (findSecurityCollateral)
                {
                    ViewBag.Exist = "This security collateral already exists!";

                    return View(securityCollateral);
                }

                securityCollateral.CreatedAt = DateTime.Now;
                securityCollateral.CreatedBy = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();

                _context.Entry(securityCollateral).State = EntityState.Added;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(securityCollateral);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var selectedSecurityCollateral = await _context.SecurityCollaterals.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedSecurityCollateral == null)
                return NotFound();

            return View(selectedSecurityCollateral);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SecurityCollateral securityCollateral)
        {
            if (ModelState.IsValid)
            {
                var securityCollateralList = await _context.SecurityCollaterals.Where(x => x.Id != securityCollateral.Id).ToListAsync();

                var findSecurityCollateral = securityCollateralList
                    .Any(x => x.SecurityCollateralName.ToLower() == securityCollateral.SecurityCollateralName.ToLower());

                if (findSecurityCollateral)
                {
                    ViewBag.Exist = "This security collateral already exists!";

                    return View(securityCollateral);
                }

                var sec = _context.Entry(securityCollateral);
                sec.State = EntityState.Modified;
                sec.Property(x => x.CreatedAt).IsModified = false;
                sec.Property(x => x.CreatedBy).IsModified = false;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(securityCollateral);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var selectedSecurityCollateral = await _context.SecurityCollaterals.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedSecurityCollateral != null)
            {
                _context.SecurityCollaterals.Remove(selectedSecurityCollateral);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}