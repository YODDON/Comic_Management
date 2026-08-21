using System;
using System.Threading;
using System.Threading.Tasks;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Categories.Commands
{
    public class DeleteCategoryCommand : IRequest<ApiResponse<bool>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, ApiResponse<bool>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public DeleteCategoryCommandHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(request.Id);
            if (category == null)
            {
                return new ApiResponse<bool>(false, "Category not found", 404);
            }

            var isUsed = await _categoryRepository.IsCategoryUsedAsync(request.Id);
            if (isUsed)
            {
                return new ApiResponse<bool>(false, "Category is currently in use by one or more comics.", 400);
            }

            await _categoryRepository.DeleteCategoryAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return new ApiResponse<bool>(true, "Category deleted successfully.", 200);
        }
    }
}
