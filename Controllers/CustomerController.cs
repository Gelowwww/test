using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using System.Linq;
using System.Security.Claims;
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
    public class CustomerController : BaseController
    {
        private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;
        public CustomerController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
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

        public async Task<IActionResult> ViewCustomerDetails(int id)
        {
            var selectedCustomer = await _context.Customers.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedCustomer == null)
                return NotFound();


            return View(selectedCustomer);
        }

        public IActionResult Add()
        {
            ViewBag.BorrowerType = BorrowerTypeList();

            ViewBag.Dosri = DosriList();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(Customer customer)
        {
            var userName = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();

            ViewBag.BorrowerType = BorrowerTypeList();

            ViewBag.Dosri = DosriList();

            if (ModelState.IsValid)
            {
                var findCustomer = await _context.Customers.AnyAsync(x => x.CustomerName == customer.CustomerName);

                if (findCustomer)
                {
                    ViewBag.Exist = "This customer already exists!";

                    return View(customer);
                }

                customer.CreatedAt = DateTime.Now;
                customer.CreatedBy = userName;

                _context.Entry(customer).State = EntityState.Added;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(customer);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.BorrowerType = BorrowerTypeList();

            ViewBag.Dosri = DosriList();

            var selectedCustomer = await _context.Customers.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedCustomer == null)
                return NotFound();

            return View(selectedCustomer);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Customer customer)
        {
            ViewBag.BorrowerType = BorrowerTypeList();

            ViewBag.Dosri = DosriList();

            if (ModelState.IsValid)
            {
                var customerList = await _context.Customers.Where(x => x.Id != customer.Id).ToListAsync();

                var findCustomer = customerList.Any(x => x.CustomerName.ToLower() == customer.CustomerName.ToLower());

                if (findCustomer)
                {
                    ViewBag.Exist = "This customer already exists!";

                    return View(customer);
                }


                var cust = _context.Entry(customer);
                cust.State = EntityState.Modified;
                cust.Property(x => x.CreatedAt).IsModified = false;
                cust.Property(x => x.CreatedBy).IsModified = false;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(customer);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var selectedCustomer = await _context.Customers.SingleOrDefaultAsync(x => x.Id == id);

            if (selectedCustomer != null)
            {
                var checkIfused = await _context.LoanRequestCustomers.AnyAsync(x => x.CustomerId == selectedCustomer.Id);

                if (checkIfused)
                {
                    return BadRequest($"Unable to delete. Customer {selectedCustomer.CustomerName} is currently tagged under a loan request.");
                }

                _context.Customers.Remove(selectedCustomer);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
        public List<SelectListItem> BorrowerTypeList()
        {
            List<SelectListItem> borrowerType = new List<SelectListItem>();
            borrowerType.Add(new SelectListItem { Text = "", Value = "" });
            borrowerType.Add(new SelectListItem { Text = "Individual", Value = "Individual" });
            borrowerType.Add(new SelectListItem { Text = "Corporation", Value = "Corporation" });
            borrowerType.Add(new SelectListItem { Text = "Partnership", Value = "Partnership" });
            borrowerType.Add(new SelectListItem { Text = "Sole Proprietorship", Value = "Sole Proprietorship" });
            borrowerType.Add(new SelectListItem { Text = "Cooperative", Value = "Cooperative" });

            return borrowerType;
        }

        public List<SelectListItem> DosriList()
        {
            List<SelectListItem> dosri = new List<SelectListItem>();
            dosri.Add(new SelectListItem { Text = "", Value = "" });
            dosri.Add(new SelectListItem { Text = "Yes", Value = "Yes" });
            dosri.Add(new SelectListItem { Text = "No", Value = "No" });

            return dosri;
        }
    }
}