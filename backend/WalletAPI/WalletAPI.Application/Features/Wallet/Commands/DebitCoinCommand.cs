using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;

namespace WalletAPI.Application.Features.Wallet.Commands
{
    public class DebitCoinCommand : IRequest<WalletDebitResultDto>
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public Guid ReferenceId { get; set; }
        public string? Description { get; set; }
    }

    public class DebitCoinCommandHandler : IRequestHandler<DebitCoinCommand, WalletDebitResultDto>
    {
        private readonly IWalletRepository _repository;

        public DebitCoinCommandHandler(IWalletRepository repository)
        {
            _repository = repository;
        }

        public async Task<WalletDebitResultDto> Handle(DebitCoinCommand request, CancellationToken cancellationToken)
        {
            return await _repository.DebitAsync(
                request.UserId,
                request.Amount,
                request.ReferenceId,
                string.IsNullOrWhiteSpace(request.Description) ? "Chapter purchase" : request.Description);
        }
    }
}
