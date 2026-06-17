using Doclyn.Application.Documents.GenerateInsights;
using Doclyn.Application.Documents.Delete;
using Doclyn.Application.Documents.Download;
using Doclyn.Application.Documents.GetAll;
using Doclyn.Application.Documents.GetById;
using Doclyn.Application.Documents.GetExtractedData;
using Doclyn.Application.Documents.GetInsights;
using Doclyn.Application.Documents.GetLogs;
using Doclyn.Application.Documents.GetReviewFields;
using Doclyn.Application.Documents.Process;
using Doclyn.Application.Documents.Reclassify;
using Doclyn.Application.Documents.Reprocess;
using Doclyn.Application.Documents.ReprocessBatch;
using Doclyn.Application.Documents.ReprocessByFilter;
using Doclyn.Application.Documents.Restore;
using Doclyn.Application.Documents.Upload;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doclyn.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadDocumentResponse>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();

        var command = new UploadDocumentCommand(
            stream,
            file.FileName,
            file.ContentType,
            file.Length);

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<GetDocumentsResponse>> GetAll(
        [FromQuery] GetDocumentsQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetDocumentByIdResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetDocumentByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new DownloadDocumentQuery(id), cancellationToken);
        return File(response.FileStream, response.ContentType, response.FileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteDocumentCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RestoreDocumentCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/extracted-data")]
    public async Task<ActionResult<GetExtractedDataResponse>> GetExtractedData(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetExtractedDataQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}/review-fields")]
    public async Task<ActionResult<GetReviewFieldsResponse>> GetReviewFields(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetReviewFieldsQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}/insights")]
    public async Task<ActionResult<IReadOnlyList<DocumentInsightResponse>>> GetInsights(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetDocumentInsightsQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/generate-insights")]
    public async Task<ActionResult<GenerateDocumentInsightsResponse>> GenerateInsights(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GenerateDocumentInsightsCommand(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult<IReadOnlyList<GetDocumentLogResponse>>> GetLogs(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetDocumentLogsQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/process")]
    public async Task<ActionResult<ProcessDocumentResponse>> Process(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ProcessDocumentCommand(id), cancellationToken);
        return Accepted(response);
    }

    [HttpPost("{id:guid}/reprocess")]
    public async Task<ActionResult<ReprocessDocumentResponse>> Reprocess(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ReprocessDocumentCommand(id), cancellationToken);
        return Accepted(response);
    }

    [HttpPost("reprocess-batch")]
    public async Task<ActionResult<ReprocessBatchResponse>> ReprocessBatch(
        [FromBody] ReprocessBatchCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return Accepted(response);
    }

    [HttpPost("reprocess-by-filter")]
    public async Task<ActionResult<ReprocessByFilterResponse>> ReprocessByFilter(
        [FromBody] ReprocessByFilterCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return Accepted(response);
    }

    [HttpPost("{id:guid}/reclassify")]
    public async Task<ActionResult<ReclassifyDocumentResponse>> Reclassify(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ReclassifyDocumentCommand(id), cancellationToken);
        return Accepted(response);
    }
}
