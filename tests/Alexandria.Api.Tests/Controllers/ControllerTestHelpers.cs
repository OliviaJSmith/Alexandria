using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace Alexandria.Api.Tests.Controllers;

internal static class ControllerTestHelpers
{
    /// <summary>Gives a controller an HttpContext (optionally signed in) and a ProblemDetailsFactory so Problem() works.</summary>
    public static T WithContext<T>(this T controller, params Claim[] claims)
        where T : ControllerBase
    {
        var identity = claims.Length == 0 ? new ClaimsIdentity() : new ClaimsIdentity(claims, "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };

        var factory = new Mock<ProblemDetailsFactory>();
        factory
            .Setup(f => f.CreateProblemDetails(
                It.IsAny<HttpContext>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns((HttpContext _, int? status, string? title, string? type, string? detail, string? instance) =>
                new ProblemDetails { Status = status, Title = title, Type = type, Detail = detail, Instance = instance });
        factory
            .Setup(f => f.CreateValidationProblemDetails(
                It.IsAny<HttpContext>(), It.IsAny<ModelStateDictionary>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(new ValidationProblemDetails());
        controller.ProblemDetailsFactory = factory.Object;

        return controller;
    }

    public static ProblemDetails AssertProblem(IActionResult? result, int expectedStatus)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        return Assert.IsType<ProblemDetails>(objectResult.Value);
    }
}
