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
    public class RegionController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public RegionController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
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

        public async Task<IActionResult> ViewRegionDetails(int id)
        {
            var selectedRegion = await _context.Regions.ProjectTo<RegionForDetailedDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (selectedRegion == null)
                return NotFound();

            return View(selectedRegion);
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(Region regionForAdd)
        {
            if (ModelState.IsValid)
            {
                var findRegion = await _context.Regions.AnyAsync(x => x.RegionName.ToLower() == regionForAdd.RegionName.ToLower());

                if (findRegion)
                {
                    ViewBag.Exist = "This region already exists!";

                    return View(regionForAdd);
                }

                regionForAdd.CreatedAt = DateTime.Now;
                regionForAdd.CreatedBy = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();

                _context.Entry(regionForAdd).State = EntityState.Added;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(regionForAdd);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var selectedRegion = await _context.Regions.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedRegion == null)
                return NotFound();

            return View(selectedRegion);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Region region)
        {
            if (ModelState.IsValid)
            {
                var regionList = await _context.Regions.Where(x => x.Id != region.Id).ToListAsync();

                var findRegion = regionList.Any(x => x.RegionName.ToLower() == region.RegionName.ToLower());

                if (findRegion)
                {
                    ViewBag.Exist = "This region already exists!";

                    return View(region);
                }

                var reg = _context.Entry(region);
                reg.State = EntityState.Modified;
                reg.Property(x => x.CreatedAt).IsModified = false;
                reg.Property(x => x.CreatedBy).IsModified = false;
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(region);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var selectedRegion = await _context.Regions.SingleOrDefaultAsync(x => x.Id == id);

            var ifArea = await _context.Areas.AnyAsync(x => x.RegionId == id);

            if (selectedRegion == null)
                return BadRequest("This region does not exist!");

            if (ifArea)
                return BadRequest("The region cannot be deleted. There are areas within this region!");

            _context.Regions.Remove(selectedRegion);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}