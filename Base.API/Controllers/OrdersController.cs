using Base.API.DTOs;
using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels;
using Base.DAL.Models.SystemModels.Enums;
using Base.Repo.Interfaces;
using Base.Shared.DTOs;
using Base.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RepositoryProject.Specifications;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly UserManager<ApplicationUser> userManager;

        public OrdersController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            this.unitOfWork = unitOfWork;
            this.userManager = userManager;
        }


        /// <summary>
        /// Creates a new order for the logged-in customer.
        /// Status defaults to 'Pending'.
        /// </summary>
        /// <param name="dto">Order details including items and optional SalesRepId.</param>
        /// <returns>The created Order ID.</returns>
        [HttpPost]
        [Authorize(Roles = "Customer")] // Ensure only Customers can place orders
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. Get Logged-in User ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // 2. Prepare Order Items & Calculate Total
            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();
            var productRepo = unitOfWork.Repository<Product>();

            foreach (var itemDto in dto.Items)
            {
                var product = await productRepo.GetByIdAsync(itemDto.ProductId);
                if (product == null)
                    return BadRequest($"Product with ID {itemDto.ProductId} not found.");

                // Optional: Check stock availability (don't deduct yet, just validate)
                if (product.CurrentStockQuantity < itemDto.Quantity)
                {
                    return BadRequest($"Insufficient stock for product '{product.Name}'. Available: {product.CurrentStockQuantity}");
                }

                // Calculate price for this line item
                decimal itemTotal = product.SellPrice * itemDto.Quantity;
                totalAmount += itemTotal;

                // Create OrderItem (Price Snapshot)
                orderItems.Add(new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.SellPrice // Capture price at moment of order
                });
            }

            // 3. Create the Order Entity
            var order = new Order
            {
                CustomerId = userId,
                TotalAmount = totalAmount,
                Status = OrderStatus.Pending, // Default Status
                OrderItems = orderItems,      // EF Core will insert these automatically
                SalesRepId = dto.SalesRepId,   // Optional, can be null
                // CommissionAmount is calculated later by Sales Rep
            };

            // 4. Save to Database
            await unitOfWork.Repository<Order>().AddAsync(order);
            var result = await unitOfWork.CompleteAsync();

            if (result <= 0)
                return StatusCode(500, "Failed to create order.");

            return Ok(new { Message = "Order created successfully", OrderId = order.Id });
        }
        // ... (Keep CreateOrder and ConfirmOrder as they were) ...

        // =================================================================
        // FIXED: ApproveOrder (Removed TransactionDate)
        // =================================================================
        /// <summary>
        /// Approves an order, deducts stock, and generates invoices.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpPut("ApproveOrder")]
        [Authorize(Roles = "StoreManager")]
        public async Task<IActionResult> ApproveOrder([FromQuery] string orderId)
        {
            var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Get Order with Includes
            var spec = new BaseSpecification<Order>(o => o.Id == orderId);
            spec.Includes.Add(o => o.OrderItems);
            spec.Includes.Add(o => o.Customer);

            var order = await unitOfWork.Repository<Order>().GetEntityWithSpecAsync(spec);

            if (order == null) return NotFound("Order not found");

            if (order.Status != OrderStatus.Confirmed)
                return BadRequest("Order must be Confirmed by Sales Rep first.");

            // 2. Process Items & Deduct Stock
            foreach (var item in order.OrderItems)
            {
                // Retrieve Product explicitly
                var product = await unitOfWork.Repository<Product>().GetByIdAsync(item.ProductId);

                if (product.CurrentStockQuantity < item.Quantity)
                {
                    return BadRequest($"Insufficient stock for product '{product.Name}'");
                }

                // Deduct Stock
                product.CurrentStockQuantity -= item.Quantity;
                await unitOfWork.Repository<Product>().UpdateAsync(product);

                // Create Stock Transaction Log
                var stockLog = new StockTransaction
                {
                    ProductId = item.ProductId,
                    OrderId = order.Id,
                    StoreManagerId = managerId,
                    Type = TransactionType.StockOut,
                    Quantity = -item.Quantity
                    // REMOVED: TransactionDate = DateTime.UtcNow 
                    // Your AppDbContext automatically fills 'DateOfCreation' which serves as the date.
                };
                await unitOfWork.Repository<StockTransaction>().AddAsync(stockLog);
            }

            // 3. Generate Customer Invoice
            var custInvoice = new Invoice
            {
                OrderId = order.Id,
                Type = InvoiceType.CustomerInvoice,
                RecipientName = order.Customer.FullName,
                Amount = order.TotalAmount,
                GeneratedDate = DateTime.UtcNow
            };
            await unitOfWork.Repository<Invoice>().AddAsync(custInvoice);

            // 4. Generate Commission Invoice (if Sales Rep exists)
            if (order.SalesRepId != null)
            {
                var salesRep = await userManager.FindByIdAsync(order.SalesRepId);

                var commInvoice = new Invoice
                {
                    OrderId = order.Id,
                    Type = InvoiceType.CommissionInvoice,
                    RecipientName = salesRep?.FullName ?? "Sales Rep",
                    Amount = order.CommissionAmount,
                    GeneratedDate = DateTime.UtcNow
                };
                await unitOfWork.Repository<Invoice>().AddAsync(commInvoice);
            }

            // 5. Finalize Order
            order.Status = OrderStatus.Approved;
            order.ApprovedDate = DateTime.UtcNow;
            await unitOfWork.Repository<Order>().UpdateAsync(order);

            // 6. Commit all changes
            var result = await unitOfWork.CompleteAsync();

            if (result <= 0) return StatusCode(500, "Failed to approve order");

            return Ok(new { Message = "Order Approved and Stock Deducted" });
        }
    }
}