using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.Dog;
using HealhtyPaws.Models.Responses.Dog;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.WebApi.Controllers.BaseControllers;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using HealthyPaws.Services.Dog;

namespace HealthyPaws.WebApi.Controllers;

public class DogsController(IDogService service) : BaseCrudController<DogInsertRequest, DogInsertRequest, DogResponse, DogSearchObject, IDogService>(service)
{
    [HttpGet]
    public override async Task<PagedResult<DogResponse>> Get(DogSearchObject searchObject)
    {
        return await base.Get(searchObject);
    }

    [HttpPost]
    public override async Task<IActionResult> Insert(DogInsertRequest request)
    {
        return await base.Insert(request);
    }
}