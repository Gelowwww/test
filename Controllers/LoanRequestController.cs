using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
    public class LoanRequestController : BaseController
    {
       private readonly CustomSignInManager<AppUser> _customSignInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private ISession _session;
        private readonly SignInManager<AppUser> _signInManager;

        public LoanRequestController(DataContext context, IHttpContextAccessor contextAccessor, IMapper mapper, CustomSignInManager<AppUser> customSignInManager,UserManager<AppUser> userManager, RoleManager<Role> roleManager, SignInManager<AppUser> signInManager) : base(context, mapper)
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

        public IActionResult Old()
        {
            return View();
        }

        public async Task<IActionResult> ViewCrmDetails(int id)
        {
            var selectedCrm = await _context.Crms.ProjectTo<CrmDetailedDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id == id);

            if (selectedCrm == null)
                return NotFound();

            if (!IsCrmContentAccessible(id).Result)
                return Unauthorized();

            var lastTatId = selectedCrm.Tats.OrderByDescending(x => x.StatusEffectiveDate).ThenByDescending(x => x.Id).Select(x => x.Id).First();
            var firstTatId = selectedCrm.Tats.OrderBy(x => x.StatusEffectiveDate).Select(x => x.Id).First();

            ViewBag.LastTatId = lastTatId;
            ViewBag.FirstTatId = firstTatId;

            return View(selectedCrm);
        }

        public async Task<IActionResult> ViewLoanRequestDetails(int id)
        {
            var selectedLr = await _context.LoanRequests.ProjectTo<LrForDetailedDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id == id);

            var getCrm = await _context.Crms.ProjectTo<CrmDetailedDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id == selectedLr.CrmId);

            if (selectedLr == null)
                return NotFound();

            if (!IsCrmContentAccessible(getCrm.Id).Result)
                return Unauthorized();

            return View(selectedLr);
        }

        public async Task<IActionResult> Add()
        {
            //init viewbag
            LrCrmDto dto = new LrCrmDto();

            var userGroupId = Convert.ToInt32(GetUserDetails()[2]);

            var group = await _context.Groups.SingleOrDefaultAsync(x => x.Id == userGroupId);

            ViewBag.CustomerList = CustomerList().Result;
            ViewBag.AnalystList = UsersList().Result;
            ViewBag.HoAnalystList = HoAnalystList().Result;
            ViewBag.RegionList = RegionList().Result;
            ViewBag.RptList = RptList();
            ViewBag.SecCollat = SecCollatList().Result;
            ViewBag.RequestTypeList = RequestTypeList();
            ViewBag.CurrencyList = CurrencyList().Result;
            ViewBag.CollatList = CollatList();
            ViewBag.FacilityList = FacilityList().Result;
            ViewBag.RequestList = RequestList().Result;
            ViewBag.PositionList = GetPositionList();

            dto.ApprovingAuthorityName = await _context.ApprovingAuthorities.Where(x => x.Id == 1).Select(x => x.ApprovingAuthorityName).SingleOrDefaultAsync();
            dto.GroupName = group.GroupName;

            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Add(LrCrmDto lrForAddDto)
        {
            var userGroupId = Convert.ToInt32(GetUserDetails()[2]);
            var userName = GetUserDetails()[0];
            var userRole = GetUserDetails()[1];

            ViewBag.CustomerList = CustomerList().Result;
            ViewBag.AnalystList = UsersList().Result;
            ViewBag.HoAnalystList = HoAnalystList().Result;
            ViewBag.RegionList = RegionList().Result;
            ViewBag.RptList = RptList();
            ViewBag.SecCollat = SecCollatList().Result;
            ViewBag.RequestTypeList = RequestTypeList();
            ViewBag.CurrencyList = CurrencyList().Result;
            ViewBag.CollatList = CollatList();
            ViewBag.FacilityList = FacilityList().Result;
            ViewBag.RequestList = RequestList().Result;
            ViewBag.PositionList = GetPositionList();

            if (ModelState.IsValid)
            {
                string refNo;
                string refNoCount = "";
                int nextNum = 0;
                string shortYear = DateTime.Now.ToString("yy");

                if (!await _context.Crms.AnyAsync())
                {
                    var lastOldLr = await _context.OldLoanRequests.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                    var oldRefNo = lastOldLr.RefNo;
                    string oldShortYear = oldRefNo.Split('-')[0];
                    if (oldShortYear == shortYear)
                        nextNum = Convert.ToInt32(oldRefNo.Split('-')[1]) + 1;
                    else
                        nextNum = 1;
                }
                else
                {
                    var lastCrm = await _context.Crms.OrderByDescending(x => x.Id).FirstAsync();
                    nextNum = Convert.ToInt32(lastCrm.RefNo.Split('-')[1]) + 1;
                    refNoCount = nextNum.ToString();
                }

                refNoCount = nextNum.ToString("00000");
                refNo = $"{shortYear}-{refNoCount}";

                var crmToAdd = new Crm()
                {
                    RefNo = refNo,
                    RegionName = lrForAddDto.RegionName,
                    AreaId = lrForAddDto.AreaId,
                    AreaName = await _context.Areas.Where(x => x.Id == lrForAddDto.AreaId)
                        .Select(x => x.AreaName).SingleOrDefaultAsync(),
                    DateReceived = lrForAddDto.DateReceived,
                    HoReceivedDate = lrForAddDto.HoReceivedDate,
                    HoAnalyst = lrForAddDto.HoAnalyst,
                    OldNew = lrForAddDto.OldNew,
                    IsAo = lrForAddDto.IsAo,
                    Rpt = lrForAddDto.Rpt,
                    ApprovingAuthorityName = lrForAddDto.ApprovingAuthorityName,
                    ReportDate = lrForAddDto.ReportDate,
                    DateSentToBranches = lrForAddDto.DateSentToBranches,
                    GroupId = userGroupId,
                    CreatedAt = DateTime.Now.Date,
                    CreatedBy = userName
                };

                await _context.Crms.AddAsync(crmToAdd);
                await _context.SaveChangesAsync();

                var lrToAdd = new LoanRequest()
                {
                    AccountName = lrForAddDto.AccountName,
                    CollateralCategory = lrForAddDto.CollateralCategory,
                    Description = lrForAddDto.Description,
                    AmountFrom = lrForAddDto.AmountFrom,
                    AmountTo = lrForAddDto.AmountTo,
                    President = lrForAddDto.President,
                    Treasurer = lrForAddDto.Treasurer,
                    MajorStockholder = lrForAddDto.MajorStockholder,
                    DateExpiry = lrForAddDto.DateExpiry,
                    ExpiryPeriod = lrForAddDto.ExpiryPeriod,
                    AssetSize = lrForAddDto.AssetSize,
                    CollateralSharingWithCbg = lrForAddDto.CollateralSharingWithCbg,
                    CollateralSharingWithCbgDetails = lrForAddDto.CollateralSharingWithCbgDetails,
                    BranchName = lrForAddDto.BranchName,
                    RequestName = lrForAddDto.RequestName,
                    RequestType = lrForAddDto.RequestType,
                    CurrencyName = lrForAddDto.CurrencyName,
                    SecurityCollateralName = lrForAddDto.SecurityCollateralName,
                    FacilityName = lrForAddDto.FacilityName,
                    CrmId = crmToAdd.Id,
                    CreatedAt = DateTime.Now.Date,
                    CreatedBy = userName
                };

                await _context.LoanRequests.AddAsync(lrToAdd);
                await _context.SaveChangesAsync();

                var lrCustomers = new List<LoanRequestCustomer>();

                foreach (var customerId in lrForAddDto.Customers)
                {
                    lrCustomers.Add(new LoanRequestCustomer { CustomerId = customerId, LoanRequestId = lrToAdd.Id });
                }

                await _context.LoanRequestCustomers.AddRangeAsync(lrCustomers);
                await _context.SaveChangesAsync();

                var tatToAdd = new Tat()
                {
                    EntityResponsible = "Initial",
                    Position = "-",
                    Status = "For review of",
                    NextEntityResponsible = lrForAddDto.EntityResponsible,
                    NextPosition = lrForAddDto.NextPosition,
                    StatusEffectiveDate = DateTime.Now.Date,
                    TatCount = 0,
                    CrmId = crmToAdd.Id,
                };

                await _context.Tats.AddAsync(tatToAdd);
                await _context.SaveChangesAsync();

                var auditTrailToAdd = new AuditTrail()
                {
                    Analyst = userName,
                    Position = userRole,
                    Action = $"Initial Add, LR id: {lrToAdd.Id}",
                    Date = DateTime.Now,
                    CrmId = crmToAdd.Id
                };

                await _context.AuditTrails.AddAsync(auditTrailToAdd);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(lrForAddDto);
        }

        public async Task<IActionResult> EditCrm(int id)
        {
            var selectedCrm = await _context.Crms.ProjectTo<CrmForEditDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id == id);

            if (selectedCrm == null)
                return NotFound();

            if (!IsCrmContentAccessible(id).Result)
                return Unauthorized();

            if (IsUneditable(selectedCrm.Id).Result)
            {
                TempData["Uneditable"] = "This CRM, together with its loan requests, is already approved. It is not editable";

                return RedirectToAction("Index");
            }

            ViewBag.RegionList = RegionList().Result;
            ViewBag.HoAnalystList = HoAnalystList().Result;
            ViewBag.RptList = RptList();

            return View(selectedCrm);
        }

        [HttpPost]
        public async Task<IActionResult> EditCrm(CrmForEditDto crmForEditDto)
        {
            var userGroupId = Convert.ToInt32(GetUserDetails()[2]);

            if (!IsCrmContentAccessible(crmForEditDto.Id).Result)
                return Unauthorized();

            if (IsUneditable(crmForEditDto.Id).Result)
            {
                TempData["Uneditable"] = "This CRM, together with its loan requests, is already approved. It is not editable";

                return RedirectToAction("Index");
            }

            ViewBag.RegionList = RegionList().Result;
            ViewBag.HoAnalystList = HoAnalystList().Result;
            ViewBag.RptList = RptList();

            if (ModelState.IsValid)
            {
                var crm = await _context.Crms.FindAsync(crmForEditDto.Id);

                crm.RegionName = crmForEditDto.RegionName;
                crm.AreaId = crmForEditDto.AreaId;
                crm.AreaName = await _context.Areas.Where(x => x.Id == crmForEditDto.AreaId)
                    .Select(x => x.AreaName).SingleOrDefaultAsync();
                crm.HoAnalyst = crmForEditDto.HoAnalyst;
                crm.HoReceivedDate = crmForEditDto.HoReceivedDate;
                crm.OldNew = crmForEditDto.OldNew;
                crm.IsAo = crmForEditDto.IsAo;
                crm.Rpt = crmForEditDto.Rpt;
                crm.ApprovingAuthorityName = crmForEditDto.ApprovingAuthorityName;
                crm.ReportDate = crmForEditDto.ReportDate;
                crm.DateSentToBranches = crmForEditDto.DateSentToBranches;

                await _context.SaveChangesAsync();

                var auditTrailToAdd = new AuditTrail()
                {
                    Analyst = GetUserDetails()[0],
                    Position = GetUserDetails()[1],
                    Action = "Edit CRM",
                    Date = DateTime.Now,
                    CrmId = crmForEditDto.Id
                };

                await _context.AuditTrails.AddAsync(auditTrailToAdd);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(crmForEditDto);
        }

        public async Task<IActionResult> EditLr(int id)
        {
            var selectedLr = await _context.LoanRequests.ProjectTo<LrForEditDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id == id);

            if (selectedLr == null)
                return NotFound();

            if (!IsCrmContentAccessible(selectedLr.CrmId).Result)
                return Unauthorized();

            if (IsUneditable(selectedLr.CrmId).Result)
            {
                TempData["Uneditable"] = "The CRM of this loan request is already approved. It is not editable";

                return RedirectToAction("Index");
            }

            ViewBag.CustomerList = CustomerList().Result;
            ViewBag.CollatList = CollatList();
            ViewBag.SecCollat = SecCollatList().Result;
            ViewBag.RequestTypeList = RequestTypeList();
            ViewBag.CurrencyList = CurrencyList().Result;
            ViewBag.FacilityList = FacilityList().Result;
            ViewBag.RequestList = RequestList().Result;
            ViewBag.BranchList = GroupBranchList(selectedLr.AreaName).Result;

            return View(selectedLr);
        }

        [HttpPost]
        public async Task<IActionResult> EditLr(LrForEditDto lrForEditDto)
        {
            if (!IsCrmContentAccessible(lrForEditDto.CrmId).Result)
                return Unauthorized();

            if (IsUneditable(lrForEditDto.CrmId).Result)
            {
                TempData["Uneditable"] = "The CRM of this loan request is already approved. It is not editable";

                return RedirectToAction("Index");
            }
            ViewBag.CustomerList = CustomerList().Result;
            ViewBag.CollatList = CollatList();
            ViewBag.SecCollat = SecCollatList().Result;
            ViewBag.RequestTypeList = RequestTypeList();
            ViewBag.CurrencyList = CurrencyList().Result;
            ViewBag.FacilityList = FacilityList().Result;
            ViewBag.RequestList = RequestList().Result;
            ViewBag.BranchList = GroupBranchList(lrForEditDto.AreaName).Result;

            if (ModelState.IsValid)
            {
                var selectedLr = await _context.LoanRequests.FindAsync(lrForEditDto.Id);

                selectedLr.AccountName = lrForEditDto.AccountName;
                selectedLr.BranchName = lrForEditDto.BranchName;
                selectedLr.CollateralCategory = lrForEditDto.CollateralCategory;
                selectedLr.Description = lrForEditDto.Description;
                selectedLr.AmountFrom = lrForEditDto.AmountFrom;
                selectedLr.AmountTo = lrForEditDto.AmountTo;
                selectedLr.CurrencyName = lrForEditDto.CurrencyName;
                selectedLr.SecurityCollateralName = lrForEditDto.SecurityCollateralName;
                selectedLr.President = lrForEditDto.President;
                selectedLr.Treasurer = lrForEditDto.Treasurer;
                selectedLr.MajorStockholder = lrForEditDto.MajorStockholder;
                selectedLr.DateExpiry = lrForEditDto.DateExpiry;
                selectedLr.ExpiryPeriod = lrForEditDto.ExpiryPeriod;
                selectedLr.AssetSize = lrForEditDto.AssetSize;
                selectedLr.CollateralSharingWithCbg = lrForEditDto.CollateralSharingWithCbg;
                selectedLr.CollateralSharingWithCbgDetails = lrForEditDto.CollateralSharingWithCbgDetails;
                selectedLr.RequestName = lrForEditDto.RequestName;
                selectedLr.RequestType = lrForEditDto.RequestType;
                selectedLr.FacilityName = lrForEditDto.FacilityName;

                await _context.SaveChangesAsync();

                var getLrCustomers = await _context.LoanRequestCustomers.Where(x => x.LoanRequestId == lrForEditDto.Id).ToListAsync();

                _context.LoanRequestCustomers.RemoveRange(getLrCustomers);
                await _context.SaveChangesAsync();

                var lrCustomersEdited = new List<LoanRequestCustomer>();

                foreach (var customerId in lrForEditDto.Customers)
                {
                    lrCustomersEdited.Add(new LoanRequestCustomer { CustomerId = customerId, LoanRequestId = lrForEditDto.Id });
                }

                await _context.LoanRequestCustomers.AddRangeAsync(lrCustomersEdited);

                await _context.SaveChangesAsync();

                var auditTrailToAdd = new AuditTrail()
                {
                    Analyst = GetUserDetails()[0],
                    Position = GetUserDetails()[1],
                    Action = $"Edit LR id {lrForEditDto.Id}",
                    Date = DateTime.Now,
                    CrmId = lrForEditDto.CrmId
                };

                await _context.AuditTrails.AddAsync(auditTrailToAdd);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(lrForEditDto);
        }

        public async Task<IActionResult> AddFromCrm(int crmid)
        {
            var crm = await _context.Crms.SingleOrDefaultAsync(x => x.Id == crmid);

            if (crm == null)
                return NotFound();

            if (!IsCrmContentAccessible(crmid).Result)
                return Unauthorized();

            var grouparea = await _context.GroupAreas.Include(x => x.Group).Include(x => x.Area.Region).Include(x => x.Area.Branches).Where(x => x.GroupId == crm.GroupId).ToListAsync();

            var areaInGroup = grouparea.Select(x => x.Area).ToList();
            List<SelectListItem> areaList = new List<SelectListItem>();

            areaList.Add(new SelectListItem { Text = "", Value = "" });

            foreach (var area in areaInGroup)
            {
                areaList.Add(new SelectListItem { Text = area.AreaName, Value = area.AreaName });
            }

            ViewBag.AreaList = areaList;
            ViewBag.CustomerList = CustomerList().Result;
            ViewBag.CollatList = CollatList();
            ViewBag.SecCollat = SecCollatList().Result;
            ViewBag.RequestTypeList = RequestTypeList();
            ViewBag.CurrencyList = CurrencyList().Result;
            ViewBag.FacilityList = FacilityList().Result;
            ViewBag.RequestList = RequestList().Result;

            var addFromCrm = new AddFromCrmDto()
            {
                CrmId = crmid,
                RefNo = crm.RefNo,
                RegionName = crm.RegionName,
                AreaName = crm.AreaName,
                DateReceived = crm.DateReceived,
                HoReceivedDate = crm.HoReceivedDate,
                HoAnalyst = crm.HoAnalyst,
                OldNew = crm.OldNew,
                IsAo = crm.IsAo,
                Rpt = crm.Rpt,
                ApprovingAuthorityName = crm.ApprovingAuthorityName,
                GroupName = crm.Group.GroupName
            };

            return View(addFromCrm);
        }

        [HttpPost]
        public async Task<IActionResult> AddFromCrm(AddFromCrmDto addFromCrmDto)
        {
            var userGroupId = Convert.ToInt32(GetUserDetails()[2]);
            var userName = GetUserDetails()[0];
            var userRole = GetUserDetails()[1];

            ViewBag.CustomerList = CustomerList().Result;
            ViewBag.CollatList = CollatList();
            ViewBag.SecCollat = SecCollatList().Result;
            ViewBag.RequestTypeList = RequestTypeList();
            ViewBag.CurrencyList = CurrencyList().Result;
            ViewBag.FacilityList = FacilityList().Result;
            ViewBag.RequestList = RequestList().Result;

            var lrToAdd = _mapper.Map<LoanRequest>(addFromCrmDto);
            lrToAdd.CreatedAt = DateTime.Now.Date;

            if (ModelState.IsValid)
            {
                _context.Entry<LoanRequest>(lrToAdd).State = EntityState.Added;
                lrToAdd.CreatedAt = DateTime.Now.Date;
                lrToAdd.CreatedBy = GetUserDetails()[0];

                await _context.SaveChangesAsync();

                List<LoanRequestCustomer> lrCustomers = new List<LoanRequestCustomer>();

                foreach (var customerId in addFromCrmDto.Customers)
                {
                    lrCustomers.Add(new LoanRequestCustomer { LoanRequestId = lrToAdd.Id, CustomerId = customerId });
                }

                await _context.LoanRequestCustomers.AddRangeAsync(lrCustomers);

                await _context.SaveChangesAsync();

                var auditTrailToAdd = new AuditTrail()
                {
                    Analyst = userName,
                    Position = userRole,
                    Action = $"Added new LR id: {lrToAdd.Id}",
                    Date = DateTime.Now,
                    CrmId = lrToAdd.CrmId
                };

                await _context.AuditTrails.AddAsync(auditTrailToAdd);
                await _context.SaveChangesAsync();

                return RedirectToAction("ViewCrmDetails", new { id = lrToAdd.CrmId });
            }

            return View(addFromCrmDto);
        }

        [HttpDelete]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var userGroupId = Convert.ToInt32(GetUserDetails()[2]);
            var userName = GetUserDetails()[0];
            var userRole = GetUserDetails()[1];

            var selectedLoanRequest = await _context.LoanRequests.SingleOrDefaultAsync(x => x.Id == id);
            var countCrmOfLr = _context.LoanRequests.Count(x => x.CrmId == selectedLoanRequest.CrmId);

            if (countCrmOfLr <= 1)
                return BadRequest("You cannot delete this LR");

            _context.LoanRequests.Remove(selectedLoanRequest);

            await _context.SaveChangesAsync();

            var auditTrailToAdd = new AuditTrail()
            {
                Analyst = userName,
                Position = userRole,
                Action = "Delete LR",
                Date = DateTime.Now,
                CrmId = selectedLoanRequest.CrmId
            };

            await _context.AuditTrails.AddAsync(auditTrailToAdd);
            await _context.SaveChangesAsync();

            return new OkResult();
        }

        //viewbaglists
        public async Task<List<SelectListItem>> GroupBranchList(string areaName)
        {
            var list = new List<SelectListItem>();
            var userGroupId = Convert.ToInt32(GetUserDetails()[2]);

            var area = await _context.Areas.Where(x => x.AreaName.ToLower() == areaName.ToLower()).SingleOrDefaultAsync();

            var branches = await _context.Branches.Where(x => x.AreaId == area.Id).ToListAsync();

            foreach (var branch in branches)
            {
                list.Add(new SelectListItem { Text = branch.BranchName, Value = branch.BranchName });
            }

            return list;
        }

        public List<SelectListItem> RptList()
        {
            string[] list =
            {
                "","Yes - Material", "Yes - Non-Material", "No"
            };
            List<SelectListItem> rptList = new List<SelectListItem>();

            foreach (var rpt in list)
            {
                rptList.Add(new SelectListItem { Text = rpt, Value = rpt });
            }

            return rptList;
        }

        public List<SelectListItem> RequestTypeList()
        {
            string[] list =
            {
                "", "Simple", "Complicated"
            };

            List<SelectListItem> rTypeList = new List<SelectListItem>();

            foreach (var request in list)
            {
                rTypeList.Add(new SelectListItem { Text = request, Value = request });
            }

            return rTypeList;
        }

        public List<SelectListItem> CollatList()
        {
            string[] list =
            {
                "", "Secured", "Partly Secured", "Clean"
            };
            List<SelectListItem> collatList = new List<SelectListItem>();

            foreach (var collat in list)
            {
                collatList.Add(new SelectListItem { Text = collat, Value = collat });
            }

            return collatList;
        }
        public async Task<List<SelectListItem>> SecCollatList()
        {
            var secCollatList = await _context.SecurityCollaterals.OrderBy(x => x.SecurityCollateralName).AsNoTracking().ToListAsync();

            List<SelectListItem> list = new List<SelectListItem>();

            list.Add(new SelectListItem { Text = "", Value = "" });

            foreach (var collat in secCollatList)
            {
                list.Add(new SelectListItem { Text = collat.SecurityCollateralName, Value = collat.SecurityCollateralName });
            }

            return list;
        }
        public async Task<List<SelectListItem>> CurrencyList()
        {
            var currencyList = await _context.Currencies.OrderBy(x => x.Id).AsNoTracking().ToListAsync();

            List<SelectListItem> list = new List<SelectListItem>();

            // list.Add(new SelectListItem { Text = "", Value = "" });

            foreach (var currency in currencyList)
            {
                list.Add(new SelectListItem { Text = currency.CurrencyName, Value = currency.CurrencyName });
            }

            return list;
        }
        public async Task<List<SelectListItem>> FacilityList()
        {
            var facilityList = await _context.Facilities.OrderBy(x => x.FacilityName).AsNoTracking().ToListAsync();

            List<SelectListItem> list = new List<SelectListItem>();

            list.Add(new SelectListItem { Text = "", Value = "" });

            foreach (var facility in facilityList)
            {
                list.Add(new SelectListItem { Text = facility.FacilityName, Value = facility.FacilityName });
            }

            return list;
        }
        public async Task<List<SelectListItem>> RequestList()
        {
            var requestList = await _context.Requests.OrderBy(x => x.RequestName).AsNoTracking().ToListAsync();

            List<SelectListItem> list = new List<SelectListItem>();

            list.Add(new SelectListItem { Text = "", Value = "" });

            foreach (var request in requestList)
            {
                list.Add(new SelectListItem { Text = request.RequestName, Value = request.RequestName });
            }

            return list;
        }

        public async Task<List<SelectListItem>> CustomerList()
        {
            var customers = await _context.Customers.AsNoTracking().ToListAsync();
            List<SelectListItem> customerList = new List<SelectListItem>();

            foreach (var customer in customers)
            {
                customerList.Add(new SelectListItem { Text = customer.CustomerName, Value = customer.Id.ToString() });
            }

            return customerList;
        }

        public async Task<List<SelectListItem>> UsersList()
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

            var x = list.OrderBy(x => x.Value).ToList();
            x.Insert(0, new SelectListItem { Text = "", Value = "" });
            return x;
        }

        public async Task<List<SelectListItem>> HoAnalystList()
        {
            var countGroupAreas = await _context.GroupAreas.GroupBy(x => x.GroupId)
                .Select(x => new { x.Key, Count = x.Count() }).ToListAsync();

            var hoGroup = countGroupAreas.OrderByDescending(x => x.Count).FirstOrDefault();

            var getHoUsers = await _context.Users.ProjectTo<GroupUserDto>(_mapper.ConfigurationProvider)
                .Where(x => x.GroupId == hoGroup.Key).AsNoTracking().ToListAsync();

            List<SelectListItem> hoList = new List<SelectListItem>();

            hoList.Add(new SelectListItem { Text = "", Value = "" });

            foreach (var user in getHoUsers)
            {
                hoList.Add(new SelectListItem { Text = user.FullName, Value = user.FullName });
            }

            return hoList;
        }

        public async Task<List<SelectListItem>> RegionList()
        {
            var userGroupId = Convert.ToInt32(GetUserDetails()[2]);

            var regionsInGroup = await _context.Groups.ProjectTo<GroupRegionDto>(_mapper.ConfigurationProvider)
                .Where(x => x.GroupId == userGroupId).Select(x => x.RegionNames).AsNoTracking().SingleOrDefaultAsync();

            List<SelectListItem> regionList = new List<SelectListItem>();

            regionList.Add(new SelectListItem { Value = "", Text = "" });

            foreach (var region in regionsInGroup.Distinct())
            {
                regionList.Add(new SelectListItem { Value = region, Text = region });
            }

            return regionList;
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

        public async Task<bool> IsCrmContentAccessible(int crmId)
        {
            bool authorized = false;

            var userGroupId = Convert.ToInt32(GetUserDetails()[2]);
            var groupAreaOfUser = await _context.GroupAreas.ProjectTo<GroupAreaRegionDto>(_mapper.ConfigurationProvider).Where(x => x.GroupId == userGroupId).ToListAsync();
            var areasAvailable = groupAreaOfUser.Select(x => x.AreaId);

            var selectedCrm = await _context.Crms.ProjectTo<CrmForEditDto>(_mapper.ConfigurationProvider).AsNoTracking().SingleOrDefaultAsync(x => x.Id == crmId);

            if (areasAvailable.Contains(selectedCrm.AreaId))
            {
                authorized = true;

                return authorized;
            }

            return authorized;
        }

        public async Task<bool> IsUneditable(int crmId)
        {
            var token = false;

            var isApproved = await _context.Tats.AnyAsync(x => x.CrmId == crmId && x.Status == "Approved");

            string[] allowedRoles = {
                            "Admin","Credit Officer",
            };
            var currentUserRoleId = GetUserDetails()[1];

            if (!allowedRoles.Contains(currentUserRoleId) && isApproved)
            {
                token = true;
                return token;
            }

            return token;
        }

        public string[] GetUserDetails()
        {
            var userId = Convert.ToInt32(HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid).Value.ToString());
            var userGroup = _context.GroupUsers.SingleOrDefault(x => x.AppUserId == userId);
            var userName = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value.ToString();
            var userRole = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role).Value.ToString();
            var userGroupId = userGroup.GroupId.ToString();


            string[] details =
            {
                userName, userRole, userGroupId
            };

            return details;
        }
    }
}