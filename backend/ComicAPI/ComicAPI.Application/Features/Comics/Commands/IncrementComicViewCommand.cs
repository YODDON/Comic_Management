using System;
using System.Threading;
using System.Threading.Tasks;
using ComicAPI.Domain.Interfaces;
using MediatR;

namespace ComicAPI.Application.Features.Comics.Commands
{
    public class IncrementComicViewCommand : IRequest<bool>
    {
        public Guid ComicId { get; set; }
    }

    public class IncrementComicViewCommandHandler : IRequestHandler<IncrementComicViewCommand, bool>
    {
        private readonly IComicRepository _comicRepository;

        public IncrementComicViewCommandHandler(IComicRepository comicRepository)
        {
            _comicRepository = comicRepository;
        }

        public async Task<bool> Handle(IncrementComicViewCommand request, CancellationToken cancellationToken)
        {
            return await _comicRepository.IncrementViewCountAsync(request.ComicId);
        }
    }
}
