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
    // [Authorize(Roles = "Admin")]
    public class GroupController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public GroupController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
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

        public async Task<IActionResult> ViewGroupDetails(int id)
        {
            var selectedGroup = await _context.Groups.ProjectTo<GroupForDetailedDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id == id);

            if(selectedGroup == null)
                return NotFound();

            return View(selectedGroup);
        }

        public IActionResult Add()
        {
            ViewBag.AreaList = GetAreaList().Result;
            ViewBag.AuthorityList = GetAuthorityList().Result;
            ViewBag.RegionList = GetRegionList().Result;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(GroupForAddDto group)
        {
            var userName = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();
            

            ViewBag.AreaList = GetAreaList().Result;
            ViewBag.AuthorityList = GetAuthorityList().Result;
            ViewBag.RegionList = GetRegionList().Result;

            if(ModelState.IsValid)
            {
                var findGroup = await _context.Groups.AnyAsync(x => x.GroupName.ToLower() == group.GroupName.ToLower());

                if(findGroup)
                {
                    ViewBag.Exist = "This group already exists!";

                    return View(group);
                }

                var groupToAdd = new Group()
                {
                    GroupName = group.GroupName,
                    CreatedAt = DateTime.Now,
                    CreatedBy = userName
                };

                await _context.Groups.AddAsync(groupToAdd);

                await _context.SaveChangesAsync();

                //map to grouparea and groupauthority

                List<GroupArea> mapGroupArea = new List<GroupArea>();

                foreach (var area in group.Areas)
                {
                    mapGroupArea.Add(new GroupArea
                    {
                        GroupId = groupToAdd.Id,
                        AreaId = area,
                        CreatedAt = DateTime.Now,
                        CreatedBy = userName
                    });
                }

                List<GroupAuthority> mapGroupAuthority = new List<GroupAuthority>();

                foreach (var authority in group.Authorities)
                {
                    mapGroupAuthority.Add(new GroupAuthority
                    {
                        GroupId = groupToAdd.Id,
                        ApprovingAuthorityId = authority,
                        CreatedAt = DateTime.Now,
                        CreatedBy = userName
                    });
                }

                await _context.GroupAreas.AddRangeAsync(mapGroupArea);
                await _context.GroupAuthorities.AddRangeAsync(mapGroupAuthority);

                await _context.SaveChangesAsync();


                return RedirectToAction("Index");
            }

            return View(group);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.AuthorityList = GetAuthorityList().Result;
            ViewBag.RegionList = GetRegionList().Result;
            ViewBag.AreaList = GetAreaList().Result;

            var selectedGroup = await _context.Groups.ProjectTo<GroupForAddDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.GroupId == id);

            if(selectedGroup.GroupId == 3)
                return Unauthorized();

            if(selectedGroup == null)
                return NotFound();

            return View(selectedGroup);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(GroupForAddDto group)
        {
            ViewBag.AreaList = GetAreaList().Result;
            ViewBag.AuthorityList = GetAuthorityList().Result;
            ViewBag.RegionList = GetRegionList().Result;
            
            if(ModelState.IsValid)
            {
                var groupList = await _context.Groups.Where(x => x.Id != group.GroupId).ToListAsync();

                var findGroup =  groupList.Any(x => x.GroupName.ToLower() == group.GroupName.ToLower());

                if(findGroup)
                {
                    ViewBag.Exist = "This group already exists!";

                    return View(group);
                }

                var areaList = new List<GroupArea>();

                foreach (var area in group.Areas)
                {
                    areaList.Add(new GroupArea
                    {
                        GroupId = group.GroupId,
                        AreaId = area
                    });
                }
                var authorityList = new List<GroupAuthority>();

                foreach (var authority in group.Authorities)
                {
                    authorityList.Add(new GroupAuthority
                    {
                        GroupId = group.GroupId,
                        ApprovingAuthorityId = authority
                    });
                }

                var groupToEdit = await _context.Groups.Include(x => x.Areas)
                    .Include(x => x.Authorities).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == group.GroupId);

                groupToEdit.GroupName = group.GroupName;
                groupToEdit.Areas = areaList;
                groupToEdit.Authorities = authorityList;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(group);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var selectedGroup = await _context.Groups.SingleOrDefaultAsync(x => x.Id == id);

            if(selectedGroup.Id == 3)
                return BadRequest("You cannot delete Head Office!");

            if(selectedGroup != null)
            {
                _context.Groups.Remove(selectedGroup);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        public async Task<List<SelectListItem>> GetAreaList()
        {
            var areaList = new List<SelectListItem>();
            var areas = await _context.Areas.OrderBy(x => x.AreaName).AsNoTracking().ToListAsync();

            foreach (var area in areas)
            {
                areaList.Add(new SelectListItem{Text = area.AreaName, Value = area.Id.ToString()});
            }

            return areaList;
        }

        public async Task<List<SelectListItem>> GetAuthorityList()
        {
            var authorityList = new List<SelectListItem>();
            var authorities = await _context.ApprovingAuthorities.OrderBy(x => x.ApprovingAuthorityName).AsNoTracking().ToListAsync();

            foreach (var authority in authorities)
            {
                authorityList.Add(new SelectListItem{Text = authority.ApprovingAuthorityName, Value = authority.Id.ToString()});
            }

            return authorityList;
        }

        public async Task<List<SelectListItem>> GetRegionList()
        {
            var regionList = new List<SelectListItem>();
            var regions = await _context.Regions.AsNoTracking().ToListAsync();

            foreach (var region in regions)
            {
                regionList.Add(new SelectListItem{Text = region.RegionName, Value = region.RegionName});
            }

            regionList.Add(new SelectListItem{Text = "HO", Value = "HO"});

            return regionList;
        }
    }
}