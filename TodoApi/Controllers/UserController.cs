﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using TodoApi.Models;
using TodoApi.BusinessManagement;
using TodoApi.Helpers;
using TodoApi.Dtos;
using System.Threading.Tasks;

namespace TodoApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UserController : ControllerBase
    {
        private IUserServices _userService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;

        public UserController(
            IUserServices userService,
            IMapper mapper,
            IOptions<AppSettings> appSettings)
        {
            _userService = userService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        // GET: api/User/Authenticate
        /// <summary>
        /// Logs in the user.
        /// </summary>
        /// <returns>Returns the User</returns>
        /// <response code="200">Returns the list of all users</response>
        /// <response code="500">On error</response>
        [AllowAnonymous]
        [HttpPost("Authenticate")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Authenticate(UserDto userDto)
        {
            var user = await _userService.Authenticate(userDto.Username, userDto.Password);

            if (user == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            // Initializes an instance of JwtSecurityTokenHandler.
            var tokenHandler = new JwtSecurityTokenHandler();

            // Encodes a set of characters into a sequence of bytes
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);

            // Place holder for all the attributes related to the issued token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // return basic user info (without password) and token to store client side
            return Ok(new
            {
                Id = user.Id,
                Username = user.Username,
                Token = tokenString
            });
        }

        // GET: api/User/Register
        /// <summary>
        /// Register a new user.
        /// </summary>
        /// <returns>Returns nothing</returns>
        /// <response code="200">Returns nothing</response>
        /// <response code="400">On creation error</response>
        [AllowAnonymous]
        [HttpPost("Register")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register(UserDto userDto)
        {
            // map dto to entity
            var user = _mapper.Map<Users>(userDto);

            try
            {
                var newUser = await _userService.Create(user, userDto.Password);
                return Ok(newUser);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/User
        /// <summary>
        /// Returns the list of all Users.
        /// </summary>
        /// <returns>Returns the list of all Users</returns>
        /// <response code="200">Returns the list of all Users</response>
        /// <response code="404">If there are no Users</response>
        /// <response code="500">On error</response>
        [HttpGet]
        [ProducesResponseType(typeof(UserDto[]), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUsers();
            var userDtos = _mapper.Map<IList<UserDto>>(users);
            return Ok(userDtos);
        }

        // GET: api/User/5
        /// <summary>
        /// Returns the User with given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The User with the matching id</returns>
        /// <response code="200">The User with the matching id</response>
        /// <response code="404">If not User match the given id</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserDto[]), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetById(id);
            var userDto = _mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        // GET: api/User/Todo/{username}
        /// <summary>
        /// Returns the Todo Items where User has given name.
        /// </summary>
        /// <param name="name">The User name</param>
        /// <returns>A Todo Item list where User has matching name</returns>
        /// <response code="200">A TodoItem list where User has matching name</response>
        /// <response code="400">If passed parameter is of invalid type</response>
        [HttpGet("Todo/{name}")]
        [ProducesResponseType(typeof(TodoItems), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetUserTodos(string name)
        {
            var items = await _userService.GetUserTodos(name);
            return Ok(items);
        }

        // PUT: api/User/5
        /// <summary>
        /// Updates the User with given id.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT /User
        ///     {
        ///        "Id": 1,
        ///        "Username": "Item1"
        ///     }
        ///
        /// </remarks>
        /// <param name="id">The User Id</param>
        /// <param name="userDto"></param>
        /// <response code="204">Returns nothing</response>
        /// <response code="400">If id or User is invalid</response>
        [HttpPut("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Update(int id, UserDto userDto)
        {
            // map dto to entity and set id
            var user = _mapper.Map<Users>(userDto);
            user.Id = id;

            try
            {
                // save 
                await _userService.UpdateUser(user, userDto.Password);
                return Ok();
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/User/5
        /// <summary>
        /// Deletes a specific User.
        /// </summary>
        /// <param name="id">The User id</param>
        /// <response code="200">Returns nothing</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteUser(id);
            return Ok();
        }
    }
}
