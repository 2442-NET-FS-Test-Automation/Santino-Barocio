using Library.Data;
using Microsoft.AspNetCore.Mvc; // ControllerBase lives here


[ApiController]// This annotation tells ASP.NET to map this controller during app.MapControllers()
[Route("api/[controller]")] //Pretty sure this will be localhost:5051/api/Inventory as the route base
public class InventoryController : ControllerBase
{
    private readonly IInventoryRepository _repo; 

    public InventoryController(IInventoryRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<EntireInventoryDTO>> Get()
    {
        //Creates an infinite Loop when we try to serialize to JSON
        //return Ok(await _repo.GetAllAsync());

        // The fix is usinf a DTO (Data Transfer Object) (bad practice 
        //to send models as returns (or take them as arguments) to/from controller methods
        //Models are for your API, not for the front end
        var items = await _repo.GetAllAsync();

        //This is what we send back when populate it
        EntireInventoryDTO response = new();

        foreach(var item in items)
        {

            // Creating an inventoryReturnDTO
            InventoryReturnDTO i = new InventoryReturnDTO
            {
                Name = item.Product.Name,
                Sku = item.Product.Sku,
                CurrentStock = item.CurrentStock
            };

            //To then populate the EntireInventoryDTO
            response.EntireInventory.Add(i);
        }

        return Ok(response);
    }

    [HttpGet("{sku}")]
    public async Task<ActionResult<InventoryReturnDTO>> GetBySku(string sku)
    {
        var item = await _repo.GetInventoryItemBySkuAsync(sku);

        if(item is null)
        {
            return NotFound(); // returns 404
        }

        var response = new InventoryReturnDTO
        {
                Name = item.Product.Name,
                Sku = item.Product.Sku,
                CurrentStock = item.CurrentStock
        };


        return Ok(response); // returns 200
    }
}