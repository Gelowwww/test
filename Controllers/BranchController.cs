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
    public class BranchController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public BranchController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
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

        public async Task<IActionResult> ViewBranchDetails(int id)
        {
            var selectedBranch = await _context.Branches
                .ProjectTo<BranchForDetailedDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id == id);

            if (selectedBranch == null)
                return NotFound();

            return View(selectedBranch);
        }

        public IActionResult Add()
        {
            ViewBag.AreaList = AreaList().Result;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(Branch branchForAdd)
        {
            ViewBag.AreaList = AreaList().Result;

            if (ModelState.IsValid)
            {
                var findBranch = await _context.Branches.AnyAsync(x => x.BranchName.ToLower() == branchForAdd.BranchName.ToLower());

                if (findBranch)
                {
                    ViewBag.Exist = "This branch already exists!";

                    return View(branchForAdd);
                }

                var findBranchCode = await _context.Branches.AnyAsync(x => x.BranchCode == branchForAdd.BranchCode);

                if (findBranchCode)
                {
                    ViewBag.Exist = "This branch code already exists!";

                    return View(branchForAdd);
                }

                branchForAdd.CreatedAt = DateTime.Now.Date;
                branchForAdd.CreatedBy = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();

                _context.Entry(branchForAdd).State = EntityState.Added;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(branchForAdd);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.AreaList = AreaList().Result;

            var selectedBranch = await _context.Branches.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedBranch == null)
                return NotFound();

            return View(selectedBranch);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Branch branch)
        {
            ViewBag.AreaList = AreaList().Result;

            if (ModelState.IsValid)
            {
                var branchList = await _context.Branches.Where(x => x.Id != branch.Id).ToListAsync();

                var findBranch = branchList.Any(x => x.BranchName.ToLower() == branch.BranchName.ToLower());

                if (findBranch)
                {
                    ViewBag.Exist = "This branch already exists!";

                    return View(branch);
                }

                var findBranchCode = branchList.Any(x => x.BranchCode == branch.BranchCode);

                if (findBranchCode)
                {
                    ViewBag.Exist = "This branch code already exists!";

                    return View(branch);
                }

                var bran = _context.Entry(branch);
                bran.State = EntityState.Modified;
                bran.Property(x => x.CreatedAt).IsModified = false;
                bran.Property(x => x.CreatedBy).IsModified = false;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(branch);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var selectedBranch = await _context.Branches.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedBranch != null)
            {
                _context.Branches.Remove(selectedBranch);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        public async Task<List<SelectListItem>> AreaList()
        {
            var areas = await _context.Areas.AsNoTracking().ToListAsync();

            List<SelectListItem> areaList = new List<SelectListItem>();

            areaList.Add(new SelectListItem { Text = "", Value = "" });

            foreach (var area in areas)
            {
                areaList.Add(new SelectListItem { Text = area.AreaName, Value = area.Id.ToString() });
            }

            return areaList;
        }
    }
}