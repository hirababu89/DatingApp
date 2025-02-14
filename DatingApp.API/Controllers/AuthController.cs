using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers 
{
    [Route ("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase 
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AuthController (IAuthRepository repo, IConfiguration config, IMapper mapper) 
        {
            _config = config;
            _mapper = mapper;
            _repo = repo;
        }

        [HttpPost ("register")]
        public async Task<IActionResult> Register (UserforRegisterDto userforRegisterDto) 
        {
            userforRegisterDto.Username = userforRegisterDto.Username.ToLower ();

            if (await _repo.UserExists (userforRegisterDto.Username))
                return BadRequest ("Username already exists");

            // var userToCreate = new User {
            //     Username = userforRegisterDto.Username
            // };
            var userToCreate = _mapper.Map<User>(userforRegisterDto);

            var createdUser = await _repo.Register (userToCreate, userforRegisterDto.Password);

            var userToReturn = _mapper.Map<UserForDetailedDto>(createdUser);

            return CreatedAtRoute("GetUser", new {controller = "Users", id = createdUser.Id}, userToReturn);// StatusCode (201);
        }

        [HttpPost ("login")]
        public async Task<IActionResult> Login (UserforLoginDto userforLoginDto) 
        {            
            // throw new Exception("Computer says no!");

            var userFromRepo = await _repo.Login (userforLoginDto.Username.ToLower(), userforLoginDto.Password);

            if (userFromRepo == null)
                return Unauthorized ();

            var claims = new [] 
            {
                new Claim (ClaimTypes.NameIdentifier, userFromRepo.Id.ToString ()),
                new Claim (ClaimTypes.Name, userFromRepo.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            var user = _mapper.Map<UserForListDto>(userFromRepo);

            return Ok(new 
            {
                token = tokenHandler.WriteToken(token),
                user
            });            
        }
    }
}