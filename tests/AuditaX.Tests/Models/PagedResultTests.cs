using AuditaX.Models;
using AuditaX.Wrappers;

namespace AuditaX.Tests.Models;

public class PagedResultTests
{
    #region Response<T> Tests

    [Fact]
    public void Response_DefaultConstructor_ShouldHaveDefaultValues()
    {
        var response = new Response<string>();

        response.Succeeded.Should().BeFalse();
        response.Data.Should().BeNull();
        response.Message.Should().BeNull();
        response.Errors.Should().BeNull();
    }

    [Fact]
    public void Response_DataConstructor_ShouldSetSucceededAndData()
    {
        var response = new Response<string>(data: "hello");

        response.Succeeded.Should().BeTrue();
        response.Data.Should().Be("hello");
        response.Message.Should().BeNull();
    }

    [Fact]
    public void Response_DataConstructor_WithMessage_ShouldSetMessage()
    {
        var response = new Response<string>("hello", "ok");

        response.Succeeded.Should().BeTrue();
        response.Data.Should().Be("hello");
        response.Message.Should().Be("ok");
    }

    [Fact]
    public void Response_MessageConstructor_ShouldSetFailure()
    {
        var response = new Response<string>("something went wrong");

        response.Succeeded.Should().BeFalse();
        response.Message.Should().Be("something went wrong");
        response.Data.Should().BeNull();
    }

    [Fact]
    public void Response_MessageAndErrorsConstructor_ShouldSetErrors()
    {
        var errors = new List<string> { "err1", "err2" };
        var response = new Response<string>("failed", errors);

        response.Succeeded.Should().BeFalse();
        response.Message.Should().Be("failed");
        response.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Response_WithAuditQueryResult_ShouldWrapCorrectly()
    {
        var items = new List<AuditQueryResult>
        {
            new() { SourceName = "Product", SourceKey = "1", AuditLog = "<xml/>" },
            new() { SourceName = "Product", SourceKey = "2", AuditLog = "<xml/>" }
        };

        var response = new Response<IEnumerable<AuditQueryResult>>(items);

        response.Succeeded.Should().BeTrue();
        response.Data.Should().HaveCount(2);
        response.Data!.First().SourceName.Should().Be("Product");
    }

    #endregion

    #region PagedResponse<T> Tests

    [Fact]
    public void PagedResponse_ShouldSetAllPaginationProperties()
    {
        var items = new List<string> { "a", "b", "c" };

        var response = new PagedResponse<IEnumerable<string>>(items, pageNumber: 2, pageSize: 10, totalCount: 50);

        response.Succeeded.Should().BeTrue();
        response.Data.Should().HaveCount(3);
        response.PageNumber.Should().Be(2);
        response.PageSize.Should().Be(10);
        response.TotalCount.Should().Be(50);
    }

    [Fact]
    public void PagedResponse_IsResponse_ShouldInheritFromResponse()
    {
        var response = new PagedResponse<IEnumerable<string>>([], 1, 10, 0);

        response.Should().BeAssignableTo<Response<IEnumerable<string>>>();
    }

    [Fact]
    public void PagedResponse_WithAuditQueryResult_ShouldWrapCorrectly()
    {
        var items = new List<AuditQueryResult>
        {
            new() { SourceName = "Product", SourceKey = "1", AuditLog = "{}" },
            new() { SourceName = "Product", SourceKey = "2", AuditLog = "{}" }
        };

        var response = new PagedResponse<IEnumerable<AuditQueryResult>>(items, pageNumber: 1, pageSize: 100, totalCount: 2);

        response.Succeeded.Should().BeTrue();
        response.Data.Should().HaveCount(2);
        response.TotalCount.Should().Be(2);
        response.PageNumber.Should().Be(1);
        response.PageSize.Should().Be(100);
    }

    [Fact]
    public void PagedResponse_WithAuditSummaryResult_ShouldWrapCorrectly()
    {
        var items = new List<AuditSummaryResult>
        {
            new() { SourceName = "Product", SourceKey = "1", LastAction = "Created", LastUser = "admin" }
        };

        var response = new PagedResponse<IEnumerable<AuditSummaryResult>>(items, pageNumber: 1, pageSize: 100, totalCount: 1);

        response.Succeeded.Should().BeTrue();
        response.Data.Should().HaveCount(1);
        response.TotalCount.Should().Be(1);
    }

    #endregion
}
