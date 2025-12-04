using System.Linq.Expressions;
using Base.API.DTOs;
using Base.DAL.Models.SystemModels;
using Base.Repo.Interfaces;
using Base.Repo.Specifications;
using BaseAPI.Validation.ProductValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RepositoryProject.Specifications;

namespace Base.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class InventoryController(IUnitOfWork unit 
   , ProductDtoValidation productValidator
   ,ProductUpdateDtoValidation productUpdateValidator
   ,ILogger<Product> logger) : ControllerBase
{

   [HttpGet("products")]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status500InternalServerError)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesResponseType(StatusCodes.Status400BadRequest)]
   public async Task<IActionResult> GetAll( ISpecification<Inventory> spec )
   {  
      if (spec.Skip < 1 || spec.Take > 100 || spec.Take < 1)
       return BadRequest();
      try
      {
         spec.Includes.Add(x => x.Products);
         
         var inventory = await unit
            .Repository<Inventory>()
            .ListAsync(spec);
         var list =
            inventory
               .Select(x => new { List = x.Products, SalesPersonId = x.SalesPersonId })
               .FirstOrDefault();
         
         var response =
            list?.List
            .Select(a => new ProductDto
            {
               ProductId = a.Id
               ,ProductName = a.Name,
               Quantity = a.CurrentStockQuantity,
               SKU = a.SKU,
               Description = a.Description
            })
            .ToList();

         if (response == null)
            return  NotFound();
         
         return Ok(new ApiResponseDTO {Data =  response, StatusCode = StatusCodes.Status200OK , Message = "OK"});
      }
      catch (Exception ex)
      {
         return StatusCode(500,new  ApiResponseDTO {Message = "Error while getting proudcts"});
      }
   }

   [HttpGet("products/{id}")]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesResponseType(StatusCodes.Status400BadRequest)]
   [ProducesResponseType(StatusCodes.Status500InternalServerError)]
   public async Task<IActionResult> GetProductSpecifications([FromRoute] string id)
   {
      if (string.IsNullOrEmpty(id))
         return BadRequest(new  ApiResponseDTO {Message = "Invalid ID"});

      try
      {
         var product = await unit
            .Repository<Product>()
            .GetByIdAsync(id);
         if (string.IsNullOrEmpty(product.Id) || string.IsNullOrEmpty(product.Name))
            return NotFound();

         var response = new ProductDto()
         {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = product.CurrentStockQuantity,
            SKU = product.SKU,
            Description = product.Description
         };

         return Ok(new ApiResponseDTO {Data = response, StatusCode = StatusCodes.Status200OK, Message = "OK"});
      }
      catch (Exception ex)
      {
         return StatusCode(500,new  ApiResponseDTO {Message = "Error while getting products"});
      }
   }

   [HttpPost("products")]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status400BadRequest)]
   [ProducesResponseType(StatusCodes.Status500InternalServerError)]
   public async Task<IActionResult> CreateProduct([FromBody] ProductDto productDto)
   {
      var validate = await productValidator.ValidateAsync(productDto);
      if (!validate.IsValid)
         return BadRequest(new  ApiResponseDTO {Message = "Invalid Input parameters"});
      try
      {
         var product = new Product()
         {
             Name = productDto.ProductName,
             SKU = productDto.SKU,
             Description = productDto.Description,
             CurrentStockQuantity = productDto.Quantity,
         };

         await unit.Repository<Product>().AddAsync(product);
         var result = await unit.CompleteAsync();
         if (result == 0)
            return BadRequest(new ApiResponseDTO { Message = "Error occured while adding product" });
         return Ok(new ApiResponseDTO {Data = result, StatusCode = StatusCodes.Status201Created});
      }
      catch (Exception ex)
      {
         return StatusCode(500,new  ApiResponseDTO {Message = "Error while creating product"});
      }
   }

   [HttpPut("products/{id}")]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status400BadRequest)]
   [ProducesResponseType(StatusCodes.Status500InternalServerError)]
   public async Task<IActionResult> UpdateProduct([FromRoute] string id, [FromBody] ProductUpdateDto productDto)
   {
      var validate = await productUpdateValidator.ValidateAsync(productDto);

      if (!validate.IsValid)
      {
         return BadRequest(new  ApiResponseDTO {Message = "Invalid Input parameters"});
      }

      try
      { 
         var product = await unit.Repository<Product>().GetByIdAsync(id);
         if (string.IsNullOrEmpty(product.Name))
            return NotFound();
         product.Name = productDto.ProductName;
         product.SKU = productDto.SKU;
         product.Description = productDto.Description;
         product.SellPrice = productDto.SellPrice;
         
         var result =  await unit.CompleteAsync();
         
         if (result == 0)
            return BadRequest(new  ApiResponseDTO { Message = "Error occured while updating product" });
         return Ok(new ApiResponseDTO {Data = result, StatusCode = StatusCodes.Status200OK});         
      }
      catch (Exception ex)
      {
         return StatusCode(500,new   ApiResponseDTO {Message = "Error while updating product"});
      }
   }

   [HttpDelete("products/{id}")]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status400BadRequest)]
   [ProducesResponseType(StatusCodes.Status500InternalServerError)]
   public async Task<IActionResult> DeleteProduct([FromRoute] string id)
   {
      if (string.IsNullOrEmpty(id))
         return BadRequest(new  ApiResponseDTO {Message = "Invalid ID"});
      try
      {
            var product = await unit.Repository<Product>().GetByIdAsync(id);

            if (String.IsNullOrEmpty(product.Name))
               return NotFound();

            product.IsDeleted = true;
            
            var result =  await unit.CompleteAsync();
            
            if (result == 0)
               return  BadRequest(new ApiResponseDTO { Message = "Error occured while deleting product" });

            return NoContent();
      }
      catch (Exception ex)
      {
         return StatusCode(500,new  ApiResponseDTO {Message = "Error while deleting product"});
      }
   }

   [HttpPost("products/stock/in")]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status400BadRequest)]
   [ProducesResponseType(StatusCodes.Status500InternalServerError)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   public async Task<IActionResult> UpdateInventoryQuantity([FromQuery]string productId,[FromQuery] int quantity)
   {
      if (string.IsNullOrEmpty(productId))
         return BadRequest(new  ApiResponseDTO {Message = "Invalid ID"});
      
      if (quantity <= 0)
         return BadRequest(new  ApiResponseDTO {Message = "Invalid Quantity"});

      try
      {
           var product =  await unit.Repository<Product>().GetByIdAsync(productId);

           if (string.IsNullOrEmpty(product.Name))
              return NotFound();
           
           product.CurrentStockQuantity = quantity;
           
           var result = await unit.CompleteAsync();
           
           if (result == 0)
              return BadRequest(new  ApiResponseDTO { Message = "Error occured while updating product" });

           return NoContent();
      }
      catch (Exception ex)
      {
         return StatusCode(500,new  ApiResponseDTO {Message = "Error while updating inventory quantity"});
      }
   }
}