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
    public class CurrencyController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public CurrencyController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
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

        public async Task<IActionResult> ViewCurrencyDetails(int id)
        {
            var selectedCurrency = await _context.Currencies.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedCurrency == null)
                return NotFound();

            return View(selectedCurrency);
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(Currency currency)
        {
            if (ModelState.IsValid)
            {
                var findCurrency = await _context.Currencies.AnyAsync(x => x.CurrencyName.ToLower() == currency.CurrencyName.ToLower());

                if (findCurrency)
                {
                    ViewBag.Exist = "This currency already exists!";

                    return View(currency);
                }

                var findCurrCode = await _context.Currencies.AnyAsync(x => x.CurrencyCode.ToLower() == currency.CurrencyCode.ToLower());

                if (findCurrCode)
                {
                    ViewBag.Exist = "This currency code already exists!";

                    return View(currency);
                }

                currency.CreatedAt = DateTime.Now;
                currency.CreatedBy = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();

                _context.Entry(currency).State = EntityState.Added;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(currency);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var selectedCurrency = await _context.Currencies.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedCurrency == null)
                return NotFound();


            return View(selectedCurrency);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Currency currency)
        {
            if (ModelState.IsValid)
            {
                var currencyList = await _context.Currencies.Where(x => x.Id != currency.Id).ToListAsync();

                var findCurrency = currencyList.Any(x => x.CurrencyName.ToLower() == currency.CurrencyName.ToLower());

                if (findCurrency)
                {
                    ViewBag.Exist = "This currency already exists!";

                    return View(currency);
                }

                var findCurrCode = currencyList.Any(x => x.CurrencyCode.ToLower() == currency.CurrencyCode.ToLower());

                if (findCurrCode)
                {
                    ViewBag.Exist = "This currency code already exists!";

                    return View(currency);
                }

                var cur = _context.Entry(currency);
                cur.State = EntityState.Modified;
                cur.Property(x => x.CreatedAt).IsModified = false;
                cur.Property(x => x.CreatedBy).IsModified = false;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(currency);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var selectedCurrency = await _context.Currencies.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedCurrency != null)
            {
                _context.Currencies.Remove(selectedCurrency);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}