using Doclyn.Application.DocumentClassIndexers.Create;
using Doclyn.Application.DocumentClassIndexers.Disable;
using Doclyn.Application.DocumentClassIndexers.GetByDocumentClass;
using Doclyn.Application.DocumentClassIndexers.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doclyn.Api.Controllers;

[ApiController]
[Route("api/document-classes/{documentClassId:guid}/indexers")]
[Authorize]
public class DocumentClassIndexersController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentClassIndexersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DocumentClassIndexerResponse>>> GetAll(
        Guid documentClassId,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new GetDocumentClassIndexersQuery(documentClassId),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DocumentClassIndexerCreatedResponse>> Create(
        Guid documentClassId,
        [FromBody] CreateDocumentClassIndexerCommand command,
        CancellationToken cancellationToken)
    {
        var commandWithClassId = command with { DocumentClassId = documentClassId };
        var response = await _mediator.Send(commandWithClassId, cancellationToken);
        return CreatedAtAction(
            nameof(GetAll),
            new { documentClassId },
            response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        Guid documentClassId,
        Guid id,
        [FromBody] UpdateDocumentClassIndexerCommand command,
        CancellationToken cancellationToken)
    {
        var commandWithIds = command with { DocumentClassId = documentClassId, Id = id };
        await _mediator.Send(commandWithIds, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Disable(
        Guid documentClassId,
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DisableDocumentClassIndexerCommand(documentClassId, id), cancellationToken);
        return NoContent();
    }
}
