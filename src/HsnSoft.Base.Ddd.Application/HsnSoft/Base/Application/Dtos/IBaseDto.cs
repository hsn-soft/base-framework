namespace HsnSoft.Base.Application.Dtos;

/// <summary>
/// The marker of a response: what a service sends to a client, and the parts of it. A response is not found by its name or its namespace but by this interface, so a controller that answers something that is not a DTO, or a response that carries an entity, can be
/// found by reflection (see <c>docs/DddBuildingBlocks.md</c>). The bases of the framework's responses (<see cref="IEntityDto{TKey}"/>, the list results) have it already; a response of another shape adds it. What a client sends (a submit, a filter) does not have to have it.
/// </summary>
public interface IBaseDto;
