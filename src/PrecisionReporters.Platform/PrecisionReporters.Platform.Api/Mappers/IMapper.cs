namespace PrecisionReporters.Platform.Api.Mappers
{
    public interface IMapper<TModel, TDto, in TCreateDto>
    {
        TModel ToModel(TDto dto);
        TModel ToModel(TCreateDto dto);
        TDto ToDto(TModel model);
    }
}
