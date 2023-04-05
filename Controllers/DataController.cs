using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using lrsms.Context;
using lrsms.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace lrsms.Controllers
{
    public class DataController : BaseController
    {
        public DataController(DataContext context, IMapper mapper) : base(context, mapper)
        {
        }

        public async Task<ContentResult> GetOldLoanRequestData()
        {
            var oldloanrequests = await _context.OldLoanRequests.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(oldloanrequests), "application/json");
        }

        public async Task<ContentResult> GetLoanRequestData()
        {
            var userGroupId = Convert.ToInt32(GetUserDetails()[2]);

            var groupAreaOfUser = await _context.GroupAreas.ProjectTo<GroupAreaRegionDto>(_mapper.ConfigurationProvider).Where(x => x.GroupId == userGroupId).AsNoTracking().ToListAsync();

            var areasAvailable = groupAreaOfUser.Select(x => x.AreaId);

            var lrData = await _context.LoanRequests.ProjectTo<LrForTableDto>(_mapper.ConfigurationProvider).Where(x => areasAvailable.Contains(x.AreaId)).AsNoTracking().ToListAsync();
            // var lrData = await _context.LoanRequests.ProjectTo<LrForTableDto>(_mapper.ConfigurationProvider).Where(x => x.GroupId == userGroupId).AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(lrData), "application/json");
        }

        public async Task<ContentResult> GetBranchesInArea()
        {
            var branchData = await _context.Branches.ProjectTo<BranchAreaDto>(_mapper.ConfigurationProvider).AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(branchData), "application/json");
        }

        public async Task<ContentResult> GetAreasInRegion()
        {
            var areaData = await _context.Areas.ProjectTo<AreaRegionDto>(_mapper.ConfigurationProvider)
                .AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(areaData), "application/json");
        }

        public async Task<ContentResult> GetCustomerData()
        {
            var customers = await _context.Customers.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(customers), "application/json");
        }

        public async Task<ContentResult> GetAdarReportData()
        {
            var adarReport = await _context.LoanRequests.ProjectTo<AdarReportDto>(_mapper.ConfigurationProvider).AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(adarReport), "application/json");
        }

        public async Task<ContentResult> GetPerAnalystReportData()
        {
            var perAnalystReport = await _context.Tats.ProjectTo<PerAnalystReportDto>(_mapper.ConfigurationProvider).Where(x => x.Position == "Analyst" || x.Position == "Credit Officer").AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(perAnalystReport), "application/json");
        }

        public async Task<ContentResult> GetTatReportData()
        {
            var tatReport = await _context.Tats.ProjectTo<TatReportDto>(_mapper.ConfigurationProvider).AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(tatReport), "application/json");
        }

        public async Task<ContentResult> GetRegionData()
        {
            var regions = await _context.Regions.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(regions), "application/json");
        }

        public async Task<ContentResult> GetAreaData()
        {
            var areas = await _context.Areas.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(areas), "application/json");
        }
        public async Task<ContentResult> GetAreaRegion()
        {
            var areaRegion = await _context.Areas.ProjectTo<RegionAreaDto>(_mapper.ConfigurationProvider).OrderBy(x => x.RegionName).AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(areaRegion), "application/json");
        }

        public async Task<ContentResult> GetBranchData()
        {
            var branches = await _context.Branches.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(branches), "application/json");
        }

        public async Task<ContentResult> GetApprovingAuthorityData()
        {
            var approvingAuthorityData = await _context.ApprovingAuthorities.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(approvingAuthorityData), "application/json");
        }

        public async Task<ContentResult> GetSecurityCollateralData()
        {
            var securityCollateralData = await _context.SecurityCollaterals.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(securityCollateralData), "application/json");
        }

        public async Task<ContentResult> GetRequestData()
        {
            var requestData = await _context.Requests.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(requestData), "application/json");
        }

        public async Task<ContentResult> GetFacilityData()
        {
            var facilityData = await _context.Facilities.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(facilityData), "application/json");
        }

        public async Task<ContentResult> GetCurrencyData()
        {
            var currencyData = await _context.Currencies.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(currencyData), "application/json");
        }

        public async Task<ContentResult> GetAuditTrailData()
        {
            var auditTrails = await _context.AuditTrails.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(auditTrails), "application/json");
        }

        public async Task<ContentResult> GetGroupData()
        {
            var groups = await _context.Groups.AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(groups), "application/json");
        }

        public async Task<ContentResult> GetUserData()
        {
            var userList = await _context.Users.ProjectTo<UserForListDto>(_mapper.ConfigurationProvider).AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(userList), "application/json");
        }

        public async Task<ContentResult> GetAccountNameData()
        {
            var accountIds = await _context.LoanRequests.ProjectTo<AccountNameDto>(_mapper.ConfigurationProvider).OrderBy(x => x.AccountName).AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(accountIds), "application/json");
        }

        public async Task<ContentResult> GetAuthoritiesFromGroup()
        {
            var authorities = await _context.GroupAreas.ProjectTo<AreaGroupAuthDto>(_mapper.ConfigurationProvider).AsNoTracking().ToListAsync();

            return Content(JsonConvert.SerializeObject(authorities), "application/json");
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