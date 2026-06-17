using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.Validation;
using Microsoft.Extensions.Options;

namespace Doclyn.UnitTests.Validation;

public sealed class FieldValidationServiceTests
{
    private readonly IFieldValidationService _service;

    public FieldValidationServiceTests()
    {
        var options = Options.Create(new FieldConfidenceOptions
        {
            ValidatedThreshold = 0.90m,
            ReviewThreshold = 0.70m,
            DefaultAiConfidence = 0.80m
        });

        _service = new FieldValidationService(options);
    }

    [Fact]
    public void Should_Return_Validated_When_Confidence_Is_Above_Validated_Threshold()
    {
        var result = _service.DetermineStatus(0.95m);
        Assert.Equal(ValidationStatus.Validated, result);
    }

    [Fact]
    public void Should_Return_Validated_At_Exact_Threshold()
    {
        var result = _service.DetermineStatus(0.90m);
        Assert.Equal(ValidationStatus.Validated, result);
    }

    [Fact]
    public void Should_Return_NeedsReview_When_Confidence_Is_Between_Thresholds()
    {
        var result = _service.DetermineStatus(0.80m);
        Assert.Equal(ValidationStatus.NeedsReview, result);
    }

    [Fact]
    public void Should_Return_NeedsReview_At_Exact_Review_Threshold()
    {
        var result = _service.DetermineStatus(0.70m);
        Assert.Equal(ValidationStatus.NeedsReview, result);
    }

    [Fact]
    public void Should_Return_Rejected_When_Confidence_Is_Below_Review_Threshold()
    {
        var result = _service.DetermineStatus(0.65m);
        Assert.Equal(ValidationStatus.Rejected, result);
    }

    [Fact]
    public void Should_Return_Rejected_For_Zero_Confidence()
    {
        var result = _service.DetermineStatus(0m);
        Assert.Equal(ValidationStatus.Rejected, result);
    }

    [Fact]
    public void Should_Return_Validated_For_Max_Confidence()
    {
        var result = _service.DetermineStatus(1.0m);
        Assert.Equal(ValidationStatus.Validated, result);
    }

    [Fact]
    public void Should_Use_Configured_Thresholds()
    {
        var customOptions = Options.Create(new FieldConfidenceOptions
        {
            ValidatedThreshold = 0.95m,
            ReviewThreshold = 0.80m,
            DefaultAiConfidence = 0.70m
        });
        var customService = new FieldValidationService(customOptions);

        Assert.Equal(ValidationStatus.Validated, customService.DetermineStatus(0.96m));
        Assert.Equal(ValidationStatus.NeedsReview, customService.DetermineStatus(0.85m));
        Assert.Equal(ValidationStatus.Rejected, customService.DetermineStatus(0.75m));
    }
}
