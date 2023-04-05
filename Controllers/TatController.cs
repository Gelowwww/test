using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using lrsms.Context;
using lrsms.Dto;
using lrsms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using lrsms.Custom;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
namespace lrsms.Controllers
{
    public class TatController : BaseController
    {
        private readonly UserManager<AppUser> _userManager;
        public TatController(DataContext context, IMapper mapper, UserManager<AppUser> userManager) : base(context, mapper)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Add(int crmId)
        {
            var getCrm = await _context.Crms.SingleOrDefaultAsync(x => x.Id == crmId);

            var isApproved = await _context.Tats.AnyAsync(x => x.CrmId == crmId && (x.Status == "Approved" || x.Status == "Denied"));

            if (isApproved)
            {
                TempData["Approved"] = "This CRM is already approved/denied. You cannot add another field in TAT.";

                return RedirectToAction("ViewCrmDetails", "LoanRequest", new { id = crmId });
            }

            TatForAddEditDto tat = new TatForAddEditDto()
            {
                CrmId = crmId,
                RefNo = getCrm.RefNo
            };

            ViewBag.UserList = GetUsers().Result;
            ViewBag.StatusList = GetStatusList();
            ViewBag.PositionList = GetPositionList();

            return View(tat);
        }

        [HttpPost]
        public async Task<IActionResult> Add(TatForAddEditDto tatForAddEdit)
        {
            var isApproved = await _context.Tats.AnyAsync(x => x.CrmId == tatForAddEdit.CrmId && x.Status == "Approved");

            if (isApproved)
                return BadRequest("This CRM is already approved/denied. You cannot add another field in TAT.");

            ViewBag.UserList = GetUsers().Result;
            ViewBag.StatusList = GetStatusList();
            ViewBag.PositionList = GetPositionList();

            if (!ModelState.IsValid)
            {
                return View(tatForAddEdit);
            }

            var lastTatRecord = await _context.Tats.Where(x => x.CrmId == tatForAddEdit.CrmId).OrderByDescending(x => x.Id).ThenByDescending(x => x.StatusEffectiveDate).FirstAsync();

            if (lastTatRecord.StatusEffectiveDate > tatForAddEdit.StatusEffectiveDate)
            {
                ViewBag.InvalidDate = "The date should be later than the previous record.";
                return View(tatForAddEdit);
            }

            tatForAddEdit.EntityResponsible = lastTatRecord.NextEntityResponsible;

            tatForAddEdit.TatCount = (tatForAddEdit.StatusEffectiveDate - lastTatRecord.StatusEffectiveDate).Days;

            var user = await _context.Users.Where(x => x.FullName == tatForAddEdit.EntityResponsible).SingleOrDefaultAsync();

            tatForAddEdit.Position = lastTatRecord.NextPosition;

            tatForAddEdit.StatusEffectiveDate = DateTime.Now.Date;

            var tat = _mapper.Map<Tat>(tatForAddEdit);

            _context.Entry<Tat>(tat).State = EntityState.Added;

            await _context.SaveChangesAsync();

            return RedirectToAction("ViewCrmDetails", "LoanRequest", new { id = tatForAddEdit.CrmId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var selectedTat = await _context.Tats.SingleOrDefaultAsync(x => x.Id == id);
            var crm = await _context.Crms.SingleOrDefaultAsync(x => x.Id == selectedTat.CrmId);
            var tatEdit = _mapper.Map<TatForAddEditDto>(selectedTat);
            tatEdit.RefNo = crm.RefNo;

            var initialTat = await _context.Tats.Where(x => x.CrmId == crm.Id).OrderBy(x => x.Id).FirstAsync();

            if (initialTat.Id == id)
            {
                TempData["Approved"] = "You cannot edit the first TAT of this CRM.";

                return RedirectToAction("ViewCrmDetails", "LoanRequest", new { id = selectedTat.CrmId });
            }

            var isApproved = await _context.Tats.AnyAsync(x => x.CrmId == selectedTat.CrmId && (x.Status == "Approved" || x.Status == "Denied"));

            if (isApproved && !isUserAccessible())
            {
                TempData["Approved"] = "This CRM is already approved/denied and cannot be edited.";

                return RedirectToAction("ViewCrmDetails", "LoanRequest", new { id = selectedTat.CrmId });
            }

            ViewBag.UserList = GetUsers().Result;
            ViewBag.StatusList = GetStatusList();
            ViewBag.PositionList = GetPositionList();

            return View(tatEdit);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TatForAddEditDto tatForEdit)
        {
            var initialTat = await _context.Tats.Where(x => x.CrmId == tatForEdit.CrmId).OrderBy(x => x.Id).AsNoTracking().FirstAsync();

            if (initialTat.Id == tatForEdit.Id)
            {
                TempData["Approved"] = "You cannot edit the first TAT of this CRM.";

                return RedirectToAction("ViewCrmDetails", "LoanRequest", new { id = tatForEdit.CrmId });
            }

            var isApproved = await _context.Tats.AnyAsync(x => x.CrmId == tatForEdit.CrmId && (x.Status == "Approved" || x.Status == "Denied"));

            if (isApproved && !isUserAccessible())
                return BadRequest("This CRM is already approved/denied and cannot be edited.");

            ViewBag.UserList = GetUsers().Result;
            ViewBag.StatusList = GetStatusList();
            ViewBag.PositionList = GetPositionList();

            if (!ModelState.IsValid)
            {
                return View(tatForEdit);
            }
            // tatForEdit.StatusEffectiveDate = DateTime.Now.Date;
            
            var lastTatRecord = await _context.Tats.Where(x => x.CrmId == tatForEdit.CrmId)
                .OrderByDescending(x => x.StatusEffectiveDate).Skip(1).AsNoTracking().FirstAsync();

            if (lastTatRecord.StatusEffectiveDate > tatForEdit.StatusEffectiveDate)
            {
                ViewBag.InvalidDate = "The date should be later than the previous record.";

                return View(tatForEdit);
            }

            tatForEdit.Position = lastTatRecord.NextPosition;

            tatForEdit.TatCount = (tatForEdit.StatusEffectiveDate - lastTatRecord.StatusEffectiveDate).Days;

            tatForEdit.EntityResponsible = lastTatRecord.NextEntityResponsible;

            var editedTat = _mapper.Map<Tat>(tatForEdit);

            _context.Entry<Tat>(editedTat).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return RedirectToAction("ViewCrmDetails", "LoanRequest", new { id = tatForEdit.CrmId });
        }

        [HttpDelete]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var selectedTat = await _context.Tats.SingleOrDefaultAsync(x => x.Id == id);
            var crmOfTat = await _context.Crms.SingleOrDefaultAsync(x => x.Id == selectedTat.CrmId);

            var lastTatRecord = crmOfTat.Tats.OrderByDescending(x => x.StatusEffectiveDate).ThenByDescending(x => x.Id).Select(x => x.Id).SingleOrDefault();

            if (selectedTat.Id != lastTatRecord || selectedTat == null)
                return BadRequest();

            _context.Tats.Remove(selectedTat);

            await _context.SaveChangesAsync();

            return RedirectToAction("ViewCrmDetails", "LoanRequest", new { id = selectedTat.CrmId });
        }

        public List<SelectListItem> GetPositionList()
        {
            var list = new List<SelectListItem>();

            string[] positions =
            {
                "-", "Analyst", "Area Head", "BRAD Division Head", "Credit Officer",
                "Department Head", "Deputy Group Head", "Group Head",
                "Region Head"
            };

            foreach (var position in positions)
            {
                list.Add(new SelectListItem { Text = position, Value = position });
            }

            return list;
        }

        public async Task<List<SelectListItem>> GetUsers()
        {
            var users = await _userManager.Users.Select(x => x.FullName).AsNoTracking().ToListAsync();
            var approvingAuth = await _context.ApprovingAuthorities.Select(x => x.ApprovingAuthorityName).AsNoTracking().ToListAsync();
            var areaHeads = await _context.Areas.Select(x => x.AreaHead).AsNoTracking().ToListAsync();
            var regionHeads = await _context.Regions.Select(x => x.RegionHead).AsNoTracking().ToListAsync();
            var entities = new List<string>()
            {
                "Branch",
                "CRECOM",
                "EXCOM",
                "Senior Management",
                "Sub-Crecom"
            };

            var combinedList = users.Union(approvingAuth).Union(areaHeads).Union(regionHeads).Union(entities);

            var list = new List<SelectListItem>();

            foreach (var item in combinedList)
            {
                list.Add(new SelectListItem { Value = item, Text = item });
            }


            return list.OrderBy(x => x.Value).ToList();
        }

        public List<SelectListItem> GetStatusList()
        {
            string[] statuses = {
                "For review of",
                "Returned to",
                "Submitted to",
                "For endorsement",
                "For approval",
                "Approved",
                "Endorsed",
                "Denied",
                "Unacted",
                "Deferred",
                "Cancelled"
            };

            statuses.OrderBy(x => x);

            List<SelectListItem> statusList = new List<SelectListItem>();

            foreach (var status in statuses)
            {
                statusList.Add(new SelectListItem { Value = status, Text = status });
            }

            return statusList;
        }

        public bool isUserAccessible()
        {
            var userRole = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role).Value.ToString();

            string[] rolesAccessible =
            {
                "Admin", "Credit Officer"
            };

            if (rolesAccessible.Contains(userRole))
                return true;

            return false;
        }
    }
}