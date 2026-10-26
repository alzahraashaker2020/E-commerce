using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers.Merchant;

/// <summary>
/// Merchant discounts CRUD. JWT-protected under <c>api/merchant/discounts</c>. A
/// duplicate code is rejected with HTTP 409 (checked via <c>GetByCodeAsync</c>).
/// </summary>
[ApiController]
[Authorize(Roles = "Merchant")]
[Route("api/merchant/discounts")]
public class MerchantDiscountsController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates a new <see cref="MerchantDiscountsController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    public MerchantDiscountsController(IRepoWrapper repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /// <summary>
    /// Lists all discounts.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DiscountVM>>>> GetAll()
    {
        List<Discount> discounts = await _repo._Discount.GetAllAsync();
        return Ok(ApiResponse<List<DiscountVM>>.Ok(_mapper.Map<List<DiscountVM>>(discounts)));
    }

    /// <summary>
    /// Returns a single discount by id, or 404 when missing.
    /// </summary>
    /// <param name="id">The discount identifier.</param>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<DiscountVM>>> GetById(int id)
    {
        Discount? discount = await _repo._Discount.GetByIdAsync(id);
        if (discount is null)
        {
            return NotFound(ApiResponse<DiscountVM>.Fail("Not found", 404));
        }

        return Ok(ApiResponse<DiscountVM>.Ok(_mapper.Map<DiscountVM>(discount)));
    }

    /// <summary>
    /// Creates a discount. Returns 409 when a discount with the same code
    /// already exists.
    /// </summary>
    /// <param name="request">The create payload.</param>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DiscountVM>>> Create(
        [FromBody] DiscountUpsertRequest request)
    {
        // Reject a duplicate code up front (the unique index also guards this).
        if (await _repo._Discount.GetByCodeAsync(request.Code) is not null)
        {
            return Conflict(ApiResponse<DiscountVM>.Fail("Discount code already exists", 409));
        }

        Discount discount = _mapper.Map<Discount>(request);
        discount.IsDemoData = false;

        _repo._Discount.Add(discount);
        _repo.Save();

        return Ok(ApiResponse<DiscountVM>.Ok(_mapper.Map<DiscountVM>(discount)));
    }

    /// <summary>
    /// Updates a discount. Returns 404 when the discount does not exist and 409
    /// when the new code collides with a different discount.
    /// </summary>
    /// <param name="id">The discount identifier.</param>
    /// <param name="request">The update payload.</param>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<DiscountVM>>> Update(
        int id,
        [FromBody] DiscountUpsertRequest request)
    {
        Discount? discount = await _repo._Discount.GetByIdAsync(id);
        if (discount is null)
        {
            return NotFound(ApiResponse<DiscountVM>.Fail("Not found", 404));
        }

        // A code change must not collide with another discount's code.
        Discount? byCode = await _repo._Discount.GetByCodeAsync(request.Code);
        if (byCode is not null && byCode.Id != id)
        {
            return Conflict(ApiResponse<DiscountVM>.Fail("Discount code already exists", 409));
        }

        _mapper.Map(request, discount);
        _repo._Discount.Update(discount);
        _repo.Save();

        return Ok(ApiResponse<DiscountVM>.Ok(_mapper.Map<DiscountVM>(discount)));
    }

    /// <summary>
    /// Deletes a discount by id using load-then-delete; returns 404 when missing.
    /// </summary>
    /// <param name="id">The discount identifier.</param>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        Discount? discount = await _repo._Discount.GetByIdAsync(id);
        if (discount is null)
        {
            return NotFound(ApiResponse<bool>.Fail("Not found", 404));
        }

        _repo._Discount.Delete(discount);
        _repo.Save();

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
