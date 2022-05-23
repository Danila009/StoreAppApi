﻿using AutoMapper;
using FastestDeliveryApi.database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreAppApi.DTOs.company;
using StoreAppApi.DTOs.product;
using StoreAppApi.models.user;
using StoreAppApi.models.сompany;
using StoreAppApi.Repository.company.banner;
using StoreAppApi.Repository.company.logo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StoreAppApi.Controllers.company
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private EfModel _efModel;
        private readonly IMapper _mapper;
        private readonly LogoCompanyRepository _logoCompanyRepository;
        private readonly BannerRepository _bannerRepository;

        public CompanyController(
            EfModel efModel, IMapper mapper,
            LogoCompanyRepository logoCompanyRepository,
            BannerRepository bannerRepository)
        {
            _bannerRepository = bannerRepository;
            _logoCompanyRepository = logoCompanyRepository;
            _efModel = efModel;
            _mapper = mapper;
        }

        [HttpGet("{id}/banner.jpg")]
        public async Task<ActionResult> GetCompanyBanner(int id)
        {
            Сompany сompany = await _efModel.Сompanies.FindAsync(id);

            if (сompany == null)
                return NotFound();

            byte[] file = _bannerRepository.GetCompanyBanner(
                сompany.Title, сompany.Id
                );

            if (file != null)
                return File(file, "image/jpeg");
            else
                return NotFound();
        }

        [Authorize(Roles = "CompanyUser")]
        [HttpPost("Banner")]
        public async Task<ActionResult> PostCompany(IFormFile banner)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
                return NotFound();

            int idUser = Convert.ToInt32(identity.FindFirst("Id").Value);

            CompanyUser companyUser = await _efModel.CompanyUsers
                .Include(u => u.Сompany)
                .FirstOrDefaultAsync(u => u.Id == idUser);

            if (companyUser == null)
                return NotFound();

            MemoryStream memoryStream = new MemoryStream();
            await banner.CopyToAsync(memoryStream);
            _bannerRepository.DeleteCompanyBanner(
                companyUser.Сompany.Title, companyUser.Сompany.Id
                );

            _bannerRepository.PostCompanyBanner(
                memoryStream.ToArray(),
                companyUser.Сompany.Title, companyUser.Сompany.Id
                );

            companyUser.Сompany.Banner = $"" +
                $"http://localhost:5000/api/Company/{companyUser.Сompany.Id}/banner.jpg";
            await _efModel.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("{id}/logo.jpg")]
        public async Task<ActionResult> GetCompanyLogo(int id)
        {
            Сompany сompany = await _efModel.Сompanies.FindAsync(id);

            if (сompany == null)
                return NotFound();

            byte[] file = _logoCompanyRepository.GetCompanyLogo(
                сompany.Title, сompany.Id
                );

            if (file != null)
                return File(file, "image/jpeg");
            else
                return NotFound();
        }

        [Authorize(Roles = "CompanyUser")]
        [HttpPost("Logo")]
        public async Task<ActionResult> PostCompanyLogo(IFormFile logo)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
                return NotFound();

            int idUser = Convert.ToInt32(identity.FindFirst("Id").Value);

            CompanyUser companyUser = await _efModel.CompanyUsers
                .Include(u => u.Сompany)
                .FirstOrDefaultAsync(u => u.Id == idUser);

            if (companyUser == null)
                return NotFound();

            MemoryStream memoryStream = new MemoryStream();
            await logo.CopyToAsync(memoryStream);
            _logoCompanyRepository.DeleteCompanyLogo(
                companyUser.Сompany.Title, companyUser.Сompany.Id
                );
            _logoCompanyRepository.PostCompanyLogo(
                memoryStream.ToArray(),
                companyUser.Сompany.Title, companyUser.Сompany.Id
                );

            companyUser.Сompany.Logo = $"" +
                $"http://localhost:5000/api/Company/{companyUser.Сompany.Id}/logo.jpg";
            await _efModel.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<ActionResult<CompanyDTO>> GetCompany()
        {
            List<Сompany> сompany = await _efModel.Сompanies.ToListAsync();

            return new CompanyDTO
            {
                Items = _mapper.Map<List<CompanyItemDTO>>(сompany)
            };
        }

        [HttpGet("{id}/Products")]
        public async Task<ActionResult<ProductDTO>> GetCompanyProduct(int id)
        {
            Сompany company = await _efModel.Сompanies
                .Include(u => u.Products)
                    .ThenInclude(u => u.Video)
                .Include(u => u.Products)
                    .ThenInclude(u => u.Images)
                .Include(u => u.Products)
                    .ThenInclude(u => u.Genre)
                .Include(u => u.Products)
                    .ThenInclude(u => u.SocialNetwork)    
                .FirstOrDefaultAsync(u => u.Id == id);

            if (company == null)
                return NotFound();

            return new ProductDTO
            {
                Items = _mapper.Map<List<ProductItemDTO>>(company.Products)
            };
        }

        [Authorize(Roles = "BaseUser")]
        [HttpPost]
        public async Task<ActionResult> PostCompany(CompanyPostDTO companyDTO)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
                return NotFound();

            int idUser = Convert.ToInt32(identity.FindFirst("Id").Value);

            var user = await _efModel.BaseUsers.FindAsync(idUser);

            if (user == null)
                return NotFound();

            CompanyUser companyUser = new CompanyUser
            {
                Сompany = new Сompany
                {
                    DateCreating = DateTime.Now,
                    Description = companyDTO.Description,
                    Title = companyDTO.Title
                },
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Password = user.Password,
                Photo = user.Photo,
                Reviews = user.Reviews,
                ProductsDownload = user.ProductsDownload
            };

            _efModel.CompanyUsers.Add(companyUser);        
            await _efModel.SaveChangesAsync();

            return Ok();
        }
    }
}
