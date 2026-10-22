using System.Text.Json;
using Monolegal.Billing.Api.Contracts;

namespace Monolegal.Billing.IntegrationTests.Api;

public class InvoiceContractSerializationTests
{
    [Fact]
    public void PagedInvoicesSerializesEachItemAsAFlatInvoice()
    {
        var dto = new PagedInvoicesDto(
            [
                new InvoiceDto(
                    "FAC-001",
                    "cliente-a",
                    "FAC-001",
                    DateTimeOffset.Parse("2026-09-01T00:00:00Z"),
                    DateTimeOffset.Parse("2026-09-10T00:00:00Z"),
                    DateTimeOffset.Parse("2026-09-02T00:00:00Z"),
                    150000m,
                    "COP",
                    "primerrecordatorio"),
            ],
            Page: 1,
            PageSize: 10,
            TotalCount: 1,
            TotalPages: 1);

        var json = JsonSerializer.Serialize(dto, JsonSerializerOptions.Web);
        using var document = JsonDocument.Parse(json);
        var item = document.RootElement.GetProperty("items")[0];

        Assert.Equal("FAC-001", item.GetProperty("number").GetString());
        Assert.Equal("cliente-a", item.GetProperty("customerId").GetString());
        Assert.Equal(150000m, item.GetProperty("amount").GetDecimal());
        Assert.Equal("primerrecordatorio", item.GetProperty("state").GetString());
        Assert.True(item.TryGetProperty("dueAt", out _));
        Assert.True(item.TryGetProperty("updatedAt", out _));
        Assert.False(item.TryGetProperty("invoice", out _));
    }
}
