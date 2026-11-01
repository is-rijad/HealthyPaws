using HealhtyPaws.Models.Requests.DogWeight;
using HealhtyPaws.Models.Responses.DogWeight;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.DogWeight;
using HealthyPaws.WebApi.Controllers.BaseControllers;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers;

public class DogWeightsController(IDogWeightService service) : BaseCrudController<DogWeightInsertRequest, DogWeightUpdateRequest, DogWeightResponse, DogWeightSearchObject, IDogWeightService>(service)
{
    [HttpGet]
    public override async Task<IActionResult> Get(DogWeightSearchObject searchObject)
    {
        return await base.Get(searchObject);
    }

    [HttpPost]
    public override async Task<IActionResult> Insert(DogWeightInsertRequest request)
    {
        return await base.Insert(request);
    }
}
