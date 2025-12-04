using System.ComponentModel.DataAnnotations;
using Base.DAL.Models.BaseModels;

namespace Base.DAL.Models.SystemModels;

public class Inventory : BaseEntity
{
   
     
    
    public string? SalesPersonId { get; set; }
    
    
    
    
    public virtual List<Product> Products { get; set; }
    
}