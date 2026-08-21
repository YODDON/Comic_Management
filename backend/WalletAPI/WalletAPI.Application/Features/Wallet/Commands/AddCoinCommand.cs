using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;
using WalletAPI.Services;

namespace WalletAPI.Application.Features.Wallet.Commands
{
    public class AddCoinCommand : IRequest<WalletCreditResultDto>
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public Guid ReferenceId { get; set; }
        public string? CreditType { get; set; }
        public string? Description { get; set; }
    }

    public class AddCoinCommandHandler : IRequestHandler<AddCoinCommand, WalletCreditResultDto>
    {
        private readonly IWalletRepository _repository;

        public AddCoinCommandHandler(IWalletRepository repository)
        {
            _repository = repository;
        }

        public async Task<WalletCreditResultDto> Handle(AddCoinCommand request, CancellationToken cancellationToken)
        {
            return await _repository.CreditAsync(
                request.UserId,
                request.Amount,
                request.ReferenceId,
                WalletRules.ResolveCreditType(request.CreditType),
                string.IsNullOrWhiteSpace(request.Description) ? "Coin credit" : request.Description);
        }
    }
}
