using Base.API.DTOs;
using Base.DAL.Models.SystemModels;
using Base.Repo.Interfaces;
using BaseAPI.Validation.SupplierValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryProject.Specifications;

namespace Base.API.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SupplierController(IUnitOfWork unit,SupplierPostValidation postValidator) : ControllerBase
{
    [HttpGet("suppliers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSuppliers(
        [FromQuery]int skip = 0
        ,[FromQuery] int take = 100)
    {
        if (skip < 0 || take < 0 || take > 100)
            return BadRequest();
        try
        {
            var spec = new BaseSpecification<Supplier>();
            spec.ApplyPaging(skip,take);
            spec.Includes.Add(x=>x.SupplyTransactions);
            var list = await unit.Repository<Supplier>()
                .ListAsync(spec);

            var response = list.Select(x=>new  SupplierReturnDto
            {
                SupplierId  = x.Id,
                Name = x.Name,
                Address = x.Address,
                ContactInfo = x.ContactInfo,
            })
            .ToList();
            return Ok(new ApiResponseDTO {Data = response,Message = "Success"});
        }
        catch (Exception ex)
        {
            return  StatusCode(500, new  ApiResponseDTO {Message = "Error getting suppliers"});
        }
    }

    [HttpPost("suppliers")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddSupplier([FromBody] SupplierPostDto supplierDto)
    {
        var result = await postValidator.ValidateAsync(supplierDto);
        if (!result.IsValid)
            return BadRequest(new  ApiResponseDTO {Message = "Invalid input parameters"});
        try
        {
            Supplier supplier = new Supplier
            {
               Name = supplierDto.Name,
               ContactInfo = supplierDto.ContactInfo,
               Address = supplierDto.Address,
            };
            await unit.Repository<Supplier>().AddAsync(supplier);
            var complete = await unit.CompleteAsync();
            if (complete <= 0)
                return BadRequest(new ApiResponseDTO { Message = "Error adding supplier" });
            return Created();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponseDTO {Message = "Error adding supplier"});
        }
    }
}