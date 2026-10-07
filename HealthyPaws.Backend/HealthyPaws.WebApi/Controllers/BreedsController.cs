using System.Threading.Tasks;
using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Responses.Breed;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.Breed;
using HealthyPaws.WebApi.Controllers.BaseControllers;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers;

public class BreedsController(IBreedService service) : BaseGetController<BreedResponse, BreedSearchObject, IBreedService>(service)
{
    [HttpGet]
    public override async Task<PagedResult<BreedResponse>> Get(BreedSearchObject searchObject)
    {
        return await base.Get(searchObject);
    }
}