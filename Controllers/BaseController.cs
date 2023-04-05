using AutoMapper;
using lrsms.Context;
using Microsoft.AspNetCore.Mvc;

namespace lrsms.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IMapper _mapper;
        protected readonly DataContext _context;
        public BaseController(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
    }
}