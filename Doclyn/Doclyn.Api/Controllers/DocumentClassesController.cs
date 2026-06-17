using Doclyn.Application.DocumentClasses.GetAll;
using Doclyn.Application.DocumentClasses.GetById;
using Doclyn.Application.DocumentClasses.GetExamples;
using Doclyn.Application.DocumentClasses.GetTop;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doclyn.Api.Controllers;

[ApiController]
[Route("api/document-classes")]
[Authorize]
public class DocumentClassesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentClassesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<GetDocumentClassesResponse>> GetAll(
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetDocumentClassesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetDocumentClassByIdResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetDocumentClassByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}/examples")]
    public async Task<ActionResult<IReadOnlyList<DocumentClassExampleResponse>>> GetExamples(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetDocumentClassExamplesQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("top")]
    public async Task<ActionResult<IReadOnlyList<GetTopDocumentClassesResponse>>> GetTop(
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new GetTopDocumentClassesQuery(take), cancellationToken);
        return Ok(response);
    }
}
