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
    public class FacilityController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public FacilityController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
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

        public async Task<IActionResult> ViewFacilityDetails(int id)
        {
            var selectedFacility = await _context.Facilities.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedFacility == null)
                return NotFound();

            return View(selectedFacility);
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(Facility facility)

        {
            if (ModelState.IsValid)
            {
                var findFacility = await _context.Facilities.AnyAsync(x => x.FacilityName.ToLower() == facility.FacilityName.ToLower());

                if (findFacility)
                {
                    ViewBag.Exist = "This request already exists!";

                    return View(facility);
                }

                facility.CreatedAt = DateTime.Now;
                facility.CreatedBy = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();

                _context.Entry(facility).State = EntityState.Added;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(facility);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var selectedFacility = await _context.Facilities.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedFacility == null)
                return NotFound();

            return View(selectedFacility);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Facility facility)
        {
            if (ModelState.IsValid)
            {
                var facilityList = await _context.Facilities.Where(x => x.Id != facility.Id).ToListAsync();

                var findRequest = facilityList.Any(x => x.FacilityName.ToLower() == facility.FacilityName.ToLower());

                if (findRequest)
                {
                    ViewBag.Exist = "This facility already exists!";

                    return View(facility);
                }

                var fac = _context.Entry(facility);
                fac.State = EntityState.Modified;
                fac.Property(x => x.CreatedAt).IsModified = false;
                fac.Property(x => x.CreatedBy).IsModified = false;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(facility);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var selectedFacility = await _context.Facilities.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedFacility != null)
            {
                _context.Facilities.Remove(selectedFacility);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}