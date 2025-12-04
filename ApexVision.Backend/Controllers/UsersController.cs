using ApexVision.Backend.DTOs;
using ApexVision.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApexVision.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UsersController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("drivers")]
        public async Task<IActionResult> GetDrivers()
        {
            var drivers = await _userManager.GetUsersInRoleAsync("Driver");
            var driverDtos = drivers.Select(d => new DriverDto
            {
                Id = d.Id.ToString(),
                FullName = d.FullName,
                PhoneNumber = d.PhoneNumber
            }).ToList();

            return Ok(driverDtos);
        }
    }
}
