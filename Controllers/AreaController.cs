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
    public class AreaController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public AreaController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
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

        public async Task<IActionResult> ViewAreaDetails(int id)
        {
            var selectedArea = await _context.Areas.ProjectTo<AreaForDetailedDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (selectedArea == null)
                return NotFound();

            return View(selectedArea);
        }

        public IActionResult Add()
        {
            ViewBag.RegionList = RegionList().Result;

            ViewBag.GroupList = GroupList().Result;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(Area areaForAdd)
        {
            ViewBag.RegionList = RegionList().Result;

            ViewBag.GroupList = GroupList().Result;

            if (ModelState.IsValid)
            {
                var findArea = await _context.Areas.AnyAsync(x => x.AreaName.ToLower() == areaForAdd.AreaName.ToLower());

                if (findArea)
                {
                    ViewBag.Exist = "This area already exists!";

                    return View(areaForAdd);
                }

                areaForAdd.CreatedAt = DateTime.Now;
                areaForAdd.CreatedBy = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();

                _context.Entry(areaForAdd).State = EntityState.Added;

                await _context.SaveChangesAsync();

                _context.GroupAreas.Add(new GroupArea { AreaId = areaForAdd.Id, GroupId = 3 });

                await _context.SaveChangesAsync();


                return RedirectToAction("Index");
            }

            return View(areaForAdd);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.RegionList = RegionList().Result;

            ViewBag.GroupList = GroupList().Result;

            var selectedArea = await _context.Areas.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedArea == null)
                return NotFound();

            return View(selectedArea);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Area area)
        {
            ViewBag.RegionList = RegionList().Result;

            ViewBag.GroupList = GroupList().Result;

            if (ModelState.IsValid)
            {
                var areaList = await _context.Areas.Where(x => x.Id != area.Id).ToListAsync();
                var findArea = areaList.Any(x => x.AreaName.ToLower() == area.AreaName.ToLower());

                if (findArea)
                {
                    ViewBag.Exist = "This area already exists!";

                    return View(area);
                }

                var are = _context.Entry(area);
                are.State = EntityState.Modified;
                are.Property(x => x.CreatedAt).IsModified = false;
                are.Property(x => x.CreatedBy).IsModified = false;

                // var getAreaId = area.Id;
                // var getGroupId = area.Group.GroupId;

                // await _context.GroupAreas.AddAsync(new GroupArea{AreaId = getAreaId, GroupId = getGroupId});

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(area);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var selectedArea = await _context.Areas.SingleOrDefaultAsync(x => x.Id == id);

            var ifCrm = await _context.Crms.AnyAsync(x => x.AreaId == id);

            if (selectedArea == null)
                return BadRequest("The area does not exist!");

            if (ifCrm)
                return BadRequest("This area is being used by a CRM!");

            _context.Areas.Remove(selectedArea);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<List<SelectListItem>> RegionList()
        {
            var regions = await _context.Regions.AsNoTracking().ToListAsync();

            List<SelectListItem> regionList = new List<SelectListItem>();

            regionList.Add(new SelectListItem { Text = "", Value = "" });

            foreach (var region in regions)
            {
                regionList.Add(new SelectListItem { Text = region.RegionName, Value = region.Id.ToString() });
            }

            return regionList;
        }

        public async Task<List<SelectListItem>> GroupList()
        {
            var groups = await _context.Groups.AsNoTracking().ToListAsync();

            List<SelectListItem> groupList = new List<SelectListItem>();

            foreach (var group in groups)
            {
                groupList.Add(new SelectListItem { Text = group.GroupName, Value = group.Id.ToString() });
            }

            return groupList;
        }
    }
}