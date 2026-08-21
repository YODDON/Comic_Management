using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PaymentAPI.DTOs;
using MediatR;
using PaymentAPI.Application.Features.Payments.Queries;
using PaymentAPI.Application.Features.Payments.Commands;
using PaymentAPI.Services;
using PaymentAPI.Settings;

namespace PaymentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("purchased-chapter")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> PurchaseChapter([FromBody] PurchaseChapterRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token." });
            }

            var response = await _mediator.Send(new PurchaseChapterCommand { UserId = userId, ChapterId = request.ChapterId });
            return StatusCode(response.StatusCode, response);
        }
        [HttpGet("transactions")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int? userId,
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out var currentUserId))
            {
                return Unauthorized();
            }

            bool isAdmin = User.IsInRole("Admin");

            if (!isAdmin && userId.HasValue && userId.Value != currentUserId)
            {
                return Forbid();
            }

            int? targetUserId = isAdmin ? userId : currentUserId;

            var response = await _mediator.Send(new GetTransactionsQuery { UserId = targetUserId, Type = type, Status = status, Search = isAdmin ? search : null, PageNumber = pageNumber, PageSize = pageSize });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("transactions/check/{id}")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> CheckTransaction(Guid id)
        {
            var response = await _mediator.Send(new GetTransactionByIdQuery { Id = id });
            if (response.Data == null)
            {
                return StatusCode(response.StatusCode, response);
            }

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out var currentUserId))
            {
                return Unauthorized();
            }

            bool isAdmin = User.IsInRole("Admin");

            if (!isAdmin && response.Data.UserId != currentUserId)
            {
                return Forbid();
            }

            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Creates a pending top-up and returns a VietQR image the payer scans to transfer.
        /// </summary>
        [HttpPost("deposit")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> CreateDeposit([FromBody] DepositRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token." });
            }

            var response = await _mediator.Send(new CreateDepositCommand { UserId = userId, Amount = request.Amount });
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// SePay's server-to-server callback: the only way a deposit gets credited.
        /// </summary>
        /// <remarks>
        /// AllowAnonymous because SePay sends no JWT — it authenticates with a shared token instead.
        /// Fails closed: if BankSettings__SepayApiKey is unset, every call is rejected. It is publicly
        /// reachable via ngrok and it credits wallets, so an unauthenticated call would let anyone
        /// forge a transfer for an amount they chose themselves.
        /// </remarks>
        [HttpPost("sepay-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> SePayWebhook(
            [FromBody] PaymentAPI.DTOs.SePayWebhookDto request,
            [FromServices] IOptions<BankSettings> bankOptions)
        {
            if (!SepayWebhookAuthenticator.IsAuthorized(
                    Request.Headers["Authorization"].ToString(), bankOptions.Value.SepayApiKey))
            {
                return Unauthorized(new { Message = "Invalid SePay webhook token." });
            }

            var response = await _mediator.Send(new ProcessSePayWebhookCommand { Request = request });

            // Always HTTP 200 once authenticated: SePay reads our verdict from the body, and a non-200
            // looks like a delivery failure that triggers pointless retries of a call we already judged.
            return Ok(response);
        }

        /// <summary>
        /// Health check for the webhook URL. SePay (and a browser test) probes the URL with a GET;
        /// without this it gets 405 and may treat the URL as invalid, so no POST is ever delivered.
        /// </summary>
        /// <remarks>
        /// Deliberately does nothing and requires no token: it only confirms the endpoint exists.
        /// No transaction is looked up, nothing is credited — crediting happens solely in the POST.
        /// </remarks>
        [HttpGet("sepay-webhook")]
        [AllowAnonymous]
        public IActionResult SePayWebhookHealthCheck() =>
            Ok(new { status = "ok", message = "SePay webhook endpoint is reachable. Send transfer notifications via POST." });
    }
}
