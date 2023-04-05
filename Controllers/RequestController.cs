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
    public class RequestController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public RequestController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
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

        public async Task<IActionResult> ViewRequestDetails(int id)
        {
            var selectedRequest = await _context.Requests.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedRequest == null)
                return NotFound();

            return View(selectedRequest);
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(Request request)
        {
            if (ModelState.IsValid)
            {
                var findRequest = await _context.Requests.AnyAsync(x => x.RequestName.ToLower() == request.RequestName.ToLower());

                if (findRequest)
                {
                    ViewBag.Exist = "This request already exists!";

                    return View(request);
                }

                request.CreatedAt = DateTime.Now;
                request.CreatedBy = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();

                _context.Entry(request).State = EntityState.Added;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(request);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var selectedRequest = await _context.Requests.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedRequest == null)
                return NotFound();

            return View(selectedRequest);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Request request)
        {
            if (ModelState.IsValid)
            {
                var requestList = await _context.Requests.Where(x => x.Id != request.Id).ToListAsync();

                var findRequest = requestList.Any(x => x.RequestName.ToLower() == request.RequestName.ToLower());

                if (findRequest)
                {
                    ViewBag.Exist = "This request already exists!";

                    return View(request);
                }

                var req = _context.Entry(request);
                req.State = EntityState.Modified;
                req.Property(x => x.CreatedAt).IsModified = false;
                req.Property(x => x.CreatedBy).IsModified = false;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(request);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var selectedRequest = await _context.Requests.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedRequest != null)
            {
                _context.Requests.Remove(selectedRequest);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}